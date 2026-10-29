<template>
  <div class="page-container">
    <div class="page-card">
      <div class="table-toolbar">
        <el-form :inline="true" :model="query" @submit.native.prevent="handleSearch">
          <el-form-item>
            <el-select v-model="query.status" placeholder="盘点状态" clearable style="width: 140px">
              <el-option label="待盘点" :value="0" />
              <el-option label="已完成" :value="1" />
              <el-option label="已取消" :value="2" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-select v-model="query.warehouseId" placeholder="盘点仓库" clearable style="width: 160px">
              <el-option v-for="item in warehouses" :key="item.id" :label="item.name" :value="item.id" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-button type="primary" icon="el-icon-search" @click="handleSearch">查询</el-button>
            <el-button icon="el-icon-refresh" @click="handleReset">重置</el-button>
          </el-form-item>
        </el-form>
        <el-button v-permission="'stocktake:edit'" type="primary" icon="el-icon-plus" @click="openCreate">新建盘点单</el-button>
      </div>

      <el-table v-loading="loading" :data="list" border stripe>
        <el-table-column prop="orderNo" label="盘点单号" width="180" />
        <el-table-column prop="warehouseName" label="盘点仓库" width="140" />
        <el-table-column prop="takeDate" label="盘点日期" width="120">
          <template slot-scope="{ row }">{{ formatDate(row.takeDate) }}</template>
        </el-table-column>
        <el-table-column prop="itemCount" label="明细数" width="90" align="center" />
        <el-table-column prop="diffCount" label="差异项" width="90" align="center">
          <template slot-scope="{ row }">
            <span :class="{ 'text-danger': row.diffCount > 0 }">{{ row.diffCount }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="100" align="center">
          <template slot-scope="{ row }">
            <el-tag :type="statusTagType(row.status)" size="mini">{{ statusName(row.status) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="createdByName" label="创建人" width="100" />
        <el-table-column prop="createdAt" label="创建时间" width="160">
          <template slot-scope="{ row }">{{ formatDateTime(row.createdAt) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="220" fixed="right">
          <template slot-scope="{ row }">
            <el-button type="text" @click="openDetail(row)">{{ row.status === 0 ? '录入实盘' : '查看' }}</el-button>
            <el-button v-if="row.status === 0" v-permission="'stocktake:execute'" type="text" @click="handleComplete(row)">完成盘点</el-button>
            <el-button v-if="row.status === 0" v-permission="'stocktake:edit'" type="text" class="text-danger" @click="handleCancel(row)">取消</el-button>
            <el-button v-if="row.status === 0" v-permission="'stocktake:edit'" type="text" class="text-danger" @click="handleDelete(row)">删除</el-button>
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
          :page-sizes="[10, 20, 50]"
          @current-change="loadData"
          @size-change="handleSearch"
        />
      </div>
    </div>

    <el-dialog title="新建盘点单" :visible.sync="createVisible" width="480px">
      <el-form ref="createForm" :model="createForm" :rules="createRules" label-width="90px">
        <el-form-item label="盘点仓库" prop="warehouseId">
          <el-select v-model="createForm.warehouseId" style="width: 100%">
            <el-option v-for="item in warehouses" :key="item.id" :label="item.name" :value="item.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="盘点日期" prop="takeDate">
          <el-date-picker v-model="createForm.takeDate" type="date" value-format="yyyy-MM-dd" style="width: 100%" />
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="createForm.remark" type="textarea" :rows="2" />
        </el-form-item>
      </el-form>
      <div class="form-tip">系统会按所选仓库的当前库存自动生成盘点明细快照。</div>
      <span slot="footer">
        <el-button @click="createVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="submitCreate">生成盘点单</el-button>
      </span>
    </el-dialog>

    <el-drawer :title="`盘点单 ${detail ? detail.orderNo : ''}`" :visible.sync="detailVisible" size="900px">
      <div v-if="detail" class="detail-body">
        <el-descriptions :column="3" border size="small">
          <el-descriptions-item label="盘点仓库">{{ detail.warehouseName }}</el-descriptions-item>
          <el-descriptions-item label="盘点日期">{{ formatDate(detail.takeDate) }}</el-descriptions-item>
          <el-descriptions-item label="状态">{{ statusName(detail.status) }}</el-descriptions-item>
          <el-descriptions-item label="备注" :span="3">{{ detail.remark || '—' }}</el-descriptions-item>
        </el-descriptions>

        <el-table :data="detail.items" border size="mini" style="margin-top: 16px">
          <el-table-column prop="materialCode" label="物料编码" width="120" />
          <el-table-column prop="materialName" label="物料名称" min-width="150" show-overflow-tooltip />
          <el-table-column prop="locationCode" label="库位" width="110" />
          <el-table-column prop="batchNo" label="批次号" width="120" />
          <el-table-column prop="systemQty" label="账面数量" width="100" align="right" />
          <el-table-column label="实盘数量" width="130">
            <template slot-scope="{ row }">
              <el-input-number
                v-if="detail.status === 0"
                v-model="row.countedQty"
                :min="0"
                :precision="2"
                :controls="false"
                size="mini"
                style="width: 100%"
                @change="row.diffQty = row.countedQty - row.systemQty"
              />
              <span v-else>{{ row.countedQty }}</span>
            </template>
          </el-table-column>
          <el-table-column label="差异" width="100" align="right">
            <template slot-scope="{ row }">
              <span :class="row.countedQty - row.systemQty !== 0 ? 'text-danger' : 'text-muted'">
                {{ row.countedQty - row.systemQty }}
              </span>
            </template>
          </el-table-column>
        </el-table>

        <div v-if="detail.status === 0" class="drawer-footer">
          <el-button @click="detailVisible = false">取消</el-button>
          <el-button type="primary" :loading="saving" @click="saveCounted">保存实盘数量</el-button>
        </div>
      </div>
    </el-drawer>
  </div>
</template>

<script>
import { stockTakeApi } from '@/api/orders'
import { warehouseApi } from '@/api/masterdata'

export default {
  name: 'StockTake',
  data() {
    return {
      loading: false,
      saving: false,
      createVisible: false,
      detailVisible: false,
      detail: null,
      list: [],
      total: 0,
      warehouses: [],
      query: { page: 1, pageSize: 20, keyword: '', status: undefined, warehouseId: undefined },
      createForm: {
        warehouseId: undefined,
        takeDate: new Date().toISOString().substring(0, 10),
        remark: ''
      },
      createRules: {
        warehouseId: [{ required: true, message: '请选择盘点仓库', trigger: 'change' }],
        takeDate: [{ required: true, message: '请选择盘点日期', trigger: 'change' }]
      }
    }
  },
  created() {
    this.loadWarehouses()
    this.loadData()
  },
  methods: {
    statusName(status) {
      return { 0: '待盘点', 1: '已完成', 2: '已取消' }[status] || '未知'
    },
    statusTagType(status) {
      return { 0: 'warning', 1: 'success', 2: 'info' }[status] || 'info'
    },
    formatDate(value) {
      return value ? value.substring(0, 10) : '—'
    },
    formatDateTime(value) {
      return value ? value.replace('T', ' ').substring(0, 19) : '—'
    },
    async loadWarehouses() {
      this.warehouses = await warehouseApi.options()
    },
    async loadData() {
      this.loading = true
      try {
        const result = await stockTakeApi.page(this.query)
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
      this.query = { page: 1, pageSize: 20, keyword: '', status: undefined, warehouseId: undefined }
      this.loadData()
    },
    openCreate() {
      this.createForm = {
        warehouseId: undefined,
        takeDate: new Date().toISOString().substring(0, 10),
        remark: ''
      }
      this.createVisible = true
      this.$nextTick(() => this.$refs.createForm && this.$refs.createForm.clearValidate())
    },
    submitCreate() {
      this.$refs.createForm.validate(async valid => {
        if (!valid) return
        this.saving = true
        try {
          await stockTakeApi.create(this.createForm)
          this.$message.success('盘点单已生成')
          this.createVisible = false
          this.loadData()
        } finally {
          this.saving = false
        }
      })
    },
    async openDetail(row) {
      this.detail = await stockTakeApi.get(row.id)
      this.detailVisible = true
    },
    saveCounted() {
      const items = this.detail.items.map(item => ({ id: item.id, countedQty: item.countedQty }))
      this.saving = true
      stockTakeApi
        .saveItems(this.detail.id, { items })
        .then(() => {
          this.$message.success('实盘数量已保存')
          this.detailVisible = false
          this.loadData()
        })
        .finally(() => {
          this.saving = false
        })
    },
    handleComplete(row) {
      this.$confirm('完成盘点后系统会按差异自动调整库存，确认继续？', '提示', { type: 'warning' })
        .then(async () => {
          await stockTakeApi.complete(row.id)
          this.$message.success('盘点完成，库存已调整')
          this.loadData()
        })
        .catch(() => {})
    },
    handleCancel(row) {
      this.$confirm(`确认取消盘点单「${row.orderNo}」？`, '提示', { type: 'warning' })
        .then(async () => {
          await stockTakeApi.cancel(row.id)
          this.$message.success('盘点单已取消')
          this.loadData()
        })
        .catch(() => {})
    },
    handleDelete(row) {
      this.$confirm(`确认删除盘点单「${row.orderNo}」？`, '提示', { type: 'warning' })
        .then(async () => {
          await stockTakeApi.remove(row.id)
          this.$message.success('删除成功')
          this.loadData()
        })
        .catch(() => {})
    }
  }
}
</script>

<style scoped>
.detail-body {
  padding: 0 20px 20px;
}
.drawer-footer {
  margin-top: 16px;
  text-align: right;
}
</style>
