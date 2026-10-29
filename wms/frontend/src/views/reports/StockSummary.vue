<template>
  <div class="page-container">
    <div class="page-card">
      <div class="table-toolbar">
        <el-form :inline="true" :model="query" @submit.native.prevent="loadData">
          <el-form-item>
            <el-input v-model="query.keyword" placeholder="物料编码 / 名称" clearable style="width: 220px" />
          </el-form-item>
          <el-form-item>
            <el-select v-model="query.warehouseId" placeholder="全部仓库" clearable style="width: 160px">
              <el-option v-for="item in warehouses" :key="item.id" :label="item.name" :value="item.id" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-button type="primary" icon="el-icon-search" @click="loadData">查询</el-button>
            <el-button icon="el-icon-refresh" @click="handleReset">重置</el-button>
          </el-form-item>
        </el-form>
        <div class="total-bar">
          合计库存金额：<strong>{{ formatNumber(totalAmount) }}</strong> 元
        </div>
      </div>

      <el-table v-loading="loading" :data="filteredList" border stripe show-summary :summary-method="summaryMethod">
        <el-table-column prop="code" label="物料编码" width="130" />
        <el-table-column prop="name" label="物料名称" min-width="160" show-overflow-tooltip />
        <el-table-column prop="spec" label="规格" min-width="130" show-overflow-tooltip />
        <el-table-column prop="categoryName" label="分类" width="120" />
        <el-table-column prop="unit" label="单位" width="70" align="center" />
        <el-table-column prop="onHandQty" label="在库数量" width="110" align="right" />
        <el-table-column prop="lockedQty" label="锁定数量" width="110" align="right" />
        <el-table-column prop="availableQty" label="可用数量" width="110" align="right" />
        <el-table-column prop="avgCost" label="平均成本" width="110" align="right" />
        <el-table-column prop="amount" label="库存金额" width="120" align="right" />
        <el-table-column prop="safetyStock" label="安全库存" width="110" align="right" />
        <el-table-column prop="alert" label="预警" width="90" align="center">
          <template slot-scope="{ row }">
            <el-tag v-if="row.alert" type="danger" size="mini">缺货</el-tag>
            <span v-else class="text-muted">正常</span>
          </template>
        </el-table-column>
      </el-table>
    </div>
  </div>
</template>

<script>
import { reportApi } from '@/api/inventory'
import { warehouseApi } from '@/api/masterdata'

export default {
  name: 'StockSummaryReport',
  data() {
    return {
      loading: false,
      list: [],
      warehouses: [],
      query: { keyword: '', warehouseId: undefined }
    }
  },
  computed: {
    filteredList() {
      const keyword = this.query.keyword.trim().toLowerCase()
      if (!keyword) return this.list
      return this.list.filter(
        item => item.code.toLowerCase().includes(keyword) || item.name.toLowerCase().includes(keyword)
      )
    },
    totalAmount() {
      return this.filteredList.reduce((sum, item) => sum + Number(item.amount || 0), 0)
    }
  },
  created() {
    this.loadWarehouses()
    this.loadData()
  },
  methods: {
    formatNumber(value) {
      return Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
    },
    async loadWarehouses() {
      this.warehouses = await warehouseApi.options()
    },
    async loadData() {
      this.loading = true
      try {
        this.list = await reportApi.stockSummary({ warehouseId: this.query.warehouseId })
      } finally {
        this.loading = false
      }
    },
    handleReset() {
      this.query = { keyword: '', warehouseId: undefined }
      this.loadData()
    },
    summaryMethod({ columns, data }) {
      const sums = []
      columns.forEach((column, index) => {
        if (index === 0) {
          sums[index] = '合计'
          return
        }
        const numericColumns = ['onHandQty', 'lockedQty', 'availableQty', 'amount']
        if (numericColumns.includes(column.property)) {
          sums[index] = this.formatNumber(data.reduce((sum, row) => sum + Number(row[column.property] || 0), 0))
          return
        }
        sums[index] = ''
      })
      return sums
    }
  }
}
</script>

<style scoped>
.total-bar {
  color: #606266;
  font-size: 13px;
}
.total-bar strong {
  color: #409eff;
  font-size: 16px;
}
</style>
