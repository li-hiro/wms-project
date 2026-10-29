<template>
  <div class="page-container">
    <el-row :gutter="16" class="summary-row">
      <el-col :xs="12" :sm="6" v-for="item in summaries" :key="item.warehouseId">
        <div class="page-card summary-card">
          <div class="summary-title">{{ item.warehouseName }}</div>
          <div class="summary-value">{{ formatNumber(item.onHandQty) }}</div>
          <div class="summary-sub">
            可用 {{ formatNumber(item.availableQty) }} · 锁定 {{ formatNumber(item.lockedQty) }}
          </div>
        </div>
      </el-col>
    </el-row>

    <div class="page-card">
      <div class="table-toolbar">
        <el-form :inline="true" :model="query" @submit.native.prevent="handleSearch">
          <el-form-item>
            <el-input v-model="query.keyword" placeholder="物料编码 / 名称 / 批次" clearable style="width: 230px" />
          </el-form-item>
          <el-form-item>
            <el-select v-model="query.warehouseId" placeholder="仓库" clearable style="width: 150px">
              <el-option v-for="item in warehouses" :key="item.id" :label="item.name" :value="item.id" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-checkbox v-model="query.onlyInStock">仅看有库存</el-checkbox>
          </el-form-item>
          <el-form-item>
            <el-checkbox v-model="query.onlyAlerts">仅看预警</el-checkbox>
          </el-form-item>
          <el-form-item>
            <el-button type="primary" icon="el-icon-search" @click="handleSearch">查询</el-button>
            <el-button icon="el-icon-refresh" @click="handleReset">重置</el-button>
          </el-form-item>
        </el-form>
      </div>

      <el-table v-loading="loading" :data="list" border stripe>
        <el-table-column prop="materialCode" label="物料编码" width="130" />
        <el-table-column prop="materialName" label="物料名称" min-width="160" show-overflow-tooltip />
        <el-table-column prop="spec" label="规格" min-width="130" show-overflow-tooltip />
        <el-table-column prop="warehouseName" label="仓库" width="120" />
        <el-table-column prop="locationCode" label="库位" width="110" />
        <el-table-column prop="batchNo" label="批次号" width="120" />
        <el-table-column prop="onHandQty" label="在库数量" width="100" align="right" />
        <el-table-column prop="lockedQty" label="锁定数量" width="100" align="right" />
        <el-table-column prop="availableQty" label="可用数量" width="100" align="right" />
        <el-table-column prop="unit" label="单位" width="70" align="center" />
        <el-table-column prop="unitCost" label="单位成本" width="100" align="right" />
        <el-table-column prop="amount" label="库存金额" width="110" align="right" />
        <el-table-column prop="alert" label="预警" width="80" align="center">
          <template slot-scope="{ row }">
            <el-tag v-if="row.alert" type="danger" size="mini">缺货</el-tag>
            <span v-else class="text-muted">正常</span>
          </template>
        </el-table-column>
        <el-table-column prop="lastInAt" label="最近入库" width="150">
          <template slot-scope="{ row }">{{ formatDateTime(row.lastInAt) }}</template>
        </el-table-column>
      </el-table>

      <div class="pagination-bar">
        <el-pagination
          background
          layout="total, sizes, prev, pager, next"
          :total="total"
          :current-page.sync="query.page"
          :page-size.sync="query.pageSize"
          :page-sizes="[10, 20, 50, 100]"
          @current-change="loadData"
          @size-change="handleSearch"
        />
      </div>
    </div>
  </div>
</template>

<script>
import { inventoryApi } from '@/api/inventory'
import { warehouseApi } from '@/api/masterdata'

export default {
  name: 'InventoryStock',
  data() {
    return {
      loading: false,
      list: [],
      total: 0,
      summaries: [],
      warehouses: [],
      query: {
        page: 1,
        pageSize: 20,
        keyword: '',
        warehouseId: undefined,
        onlyInStock: false,
        onlyAlerts: false
      }
    }
  },
  created() {
    this.loadWarehouses()
    this.loadSummary()
    this.loadData()
  },
  methods: {
    formatNumber(value) {
      return Number(value || 0).toLocaleString('zh-CN', { maximumFractionDigits: 2 })
    },
    formatDateTime(value) {
      return value ? value.replace('T', ' ').substring(0, 19) : '—'
    },
    async loadWarehouses() {
      this.warehouses = await warehouseApi.options()
    },
    async loadSummary() {
      this.summaries = await inventoryApi.summary()
    },
    async loadData() {
      this.loading = true
      try {
        const params = { ...this.query }
        if (!params.onlyInStock) delete params.onlyInStock
        if (!params.onlyAlerts) delete params.onlyAlerts
        const result = await inventoryApi.page(params)
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
      this.query = {
        page: 1,
        pageSize: 20,
        keyword: '',
        warehouseId: undefined,
        onlyInStock: false,
        onlyAlerts: false
      }
      this.loadData()
    }
  }
}
</script>

<style scoped>
.summary-row {
  margin-bottom: 16px;
}
.summary-card {
  margin-bottom: 16px;
}
.summary-title {
  color: #909399;
  font-size: 13px;
}
.summary-value {
  font-size: 22px;
  font-weight: 600;
  margin: 6px 0 4px;
}
.summary-sub {
  color: #909399;
  font-size: 12px;
}
</style>
