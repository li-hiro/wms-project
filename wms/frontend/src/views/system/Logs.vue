<template>
  <div class="page-container">
    <div class="page-card">
      <div class="table-toolbar">
        <el-form :inline="true" :model="query" @submit.native.prevent="handleSearch">
          <el-form-item>
            <el-select v-model="query.module" placeholder="模块" clearable style="width: 150px">
              <el-option v-for="item in modules" :key="item" :label="item" :value="item" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-select v-model="query.success" placeholder="执行结果" clearable style="width: 130px">
              <el-option label="成功" :value="true" />
              <el-option label="失败" :value="false" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-input v-model="query.keyword" placeholder="操作人 / 路径" clearable style="width: 200px" />
          </el-form-item>
          <el-form-item>
            <el-button type="primary" icon="el-icon-search" @click="handleSearch">查询</el-button>
            <el-button icon="el-icon-refresh" @click="handleReset">重置</el-button>
          </el-form-item>
        </el-form>
        <el-button v-permission="'system:log:view'" type="danger" plain icon="el-icon-delete" @click="handleClear">清理 30 天前日志</el-button>
      </div>

      <el-table v-loading="loading" :data="list" border stripe>
        <el-table-column prop="createdAt" label="时间" width="160">
          <template slot-scope="{ row }">{{ formatDateTime(row.createdAt) }}</template>
        </el-table-column>
        <el-table-column prop="userName" label="操作人" width="110" />
        <el-table-column prop="module" label="模块" width="110" />
        <el-table-column prop="action" label="操作" width="130" show-overflow-tooltip />
        <el-table-column prop="method" label="请求方法" width="100" />
        <el-table-column prop="path" label="请求路径" min-width="220" show-overflow-tooltip />
        <el-table-column prop="ip" label="IP" width="130" />
        <el-table-column prop="durationMs" label="耗时(ms)" width="90" align="right" />
        <el-table-column prop="success" label="结果" width="80" align="center">
          <template slot-scope="{ row }">
            <el-tag :type="row.success ? 'success' : 'danger'" size="mini">{{ row.success ? '成功' : '失败' }}</el-tag>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-bar">
        <el-pagination
          background
          layout="total, sizes, prev, pager, next"
          :total="total"
          :current-page.sync="query.page"
          :page-size.sync="query.pageSize"
          :page-sizes="[20, 50, 100]"
          @current-change="loadData"
          @size-change="handleSearch"
        />
      </div>
    </div>
  </div>
</template>

<script>
import { logApi } from '@/api/system'

export default {
  name: 'SystemLogs',
  data() {
    return {
      loading: false,
      list: [],
      total: 0,
      modules: ['物料管理', '仓库配置', '入库作业', '出库作业', '库存盘点', '库存调拨', '库存管理', '报表统计', '系统管理', '插件管理'],
      query: { page: 1, pageSize: 20, keyword: '', module: undefined, success: undefined }
    }
  },
  created() {
    this.loadData()
  },
  methods: {
    formatDateTime(value) {
      return value ? value.replace('T', ' ').substring(0, 19) : '—'
    },
    async loadData() {
      this.loading = true
      try {
        const result = await logApi.page(this.query)
        this.list = result.items
        this.total = result.total
      } finally {
        this.loading = false
      }
    },
    handleSearch() {
      this.query.page = 1
      this.loadData()
    },
    handleReset() {
      this.query = { page: 1, pageSize: 20, keyword: '', module: undefined, success: undefined }
      this.loadData()
    },
    handleClear() {
      this.$confirm('确认清理 30 天之前的操作日志？', '提示', { type: 'warning' })
        .then(async () => {
          const count = await logApi.clear(30)
          this.$message.success(`已清理 ${count} 条日志`)
          this.loadData()
        })
        .catch(() => {})
    }
  }
}
</script>
