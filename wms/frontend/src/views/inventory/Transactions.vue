<template>
  <div class="page-container">
    <div class="page-card">
      <div class="table-toolbar">
        <el-form :inline="true" :model="query" @submit.native.prevent="handleSearch">
          <el-form-item>
            <el-select v-model="query.type" placeholder="流水类型" clearable style="width: 140px">
              <el-option v-for="item in typeOptions" :key="item.value" :label="item.label" :value="item.value" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-input v-model="query.keyword" placeholder="物料 / 关联单号" clearable style="width: 220px" />
          </el-form-item>
          <el-form-item>
            <el-select v-model="query.warehouseId" placeholder="仓库" clearable style="width: 150px">
              <el-option v-for="item in warehouses" :key="item.id" :label="item.name" :value="item.id" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-date-picker
              v-model="dateRange"
              type="daterange"
              value-format="yyyy-MM-dd"
              range-separator="至"
              start-placeholder="开始日期"
              end-placeholder="结束日期"
              style="width: 260px"
            />
          </el-form-item>
          <el-form-item>
            <el-button type="primary" icon="el-icon-search" @click="handleSearch">查询</el-button>
            <el-button icon="el-icon-refresh" @click="handleReset">重置</el-button>
          </el-form-item>
        </el-form>
      </div>

      <el-table v-loading="loading" :data="list" border stripe>
        <el-table-column prop="occurredAt" label="发生时间" width="160">
          <template slot-scope="{ row }">{{ formatDateTime(row.occurredAt) }}</template>
        </el-table-column>
        <el-table-column prop="type" label="类型" width="100">
          <template slot-scope="{ row }">
            <el-tag :type="typeTagType(row.type)" size="mini">{{ typeName(row.type) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="materialCode" label="物料编码" width="130" />
        <el-table-column prop="materialName" label="物料名称" min-width="150" show-overflow-tooltip />
        <el-table-column prop="warehouseName" label="仓库" width="120" />
        <el-table-column prop="locationCode" label="库位" width="110" />
        <el-table-column prop="batchNo" label="批次号" width="120" />
        <el-table-column prop="quantity" label="变动数量" width="100" align="right">
          <template slot-scope="{ row }">
            <span :class="row.quantity >= 0 ? 'text-success' : 'text-danger'">{{ row.quantity }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="lockedQuantity" label="锁定变动" width="100" align="right" />
        <el-table-column prop="balanceQty" label="结存" width="100" align="right" />
        <el-table-column prop="refNo" label="关联单号" width="180" show-overflow-tooltip />
        <el-table-column prop="operatorName" label="操作人" width="100" />
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
  name: 'InventoryTransactions',
  data() {
    return {
      loading: false,
      list: [],
      total: 0,
      warehouses: [],
      dateRange: [],
      query: { page: 1, pageSize: 20, keyword: '', type: undefined, warehouseId: undefined },
      typeOptions: [
        { label: '入库', value: 1 },
        { label: '出库', value: 2 },
        { label: '调拨入', value: 3 },
        { label: '调拨出', value: 4 },
        { label: '锁定库存', value: 5 },
        { label: '释放锁定', value: 6 },
        { label: '盘点调整', value: 7 },
        { label: '期初导入', value: 8 }
      ]
    }
  },
  created() {
    this.loadWarehouses()
    this.loadData()
  },
  methods: {
    typeName(type) {
      return { 1: '入库', 2: '出库', 3: '调拨入', 4: '调拨出', 5: '锁定库存', 6: '释放锁定', 7: '盘点调整', 8: '期初导入' }[type] || '其他'
    },
    typeTagType(type) {
      return { 1: 'success', 2: 'danger', 3: 'success', 4: 'warning', 5: 'warning', 6: 'info', 7: 'warning', 8: 'info' }[type] || 'info'
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
        const params = { ...this.query }
        if (this.dateRange && this.dateRange.length === 2) {
          params.startDate = this.dateRange[0]
          params.endDate = this.dateRange[1]
        }
        const result = await inventoryApi.transactions(params)
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
      this.query = { page: 1, pageSize: 20, keyword: '', type: undefined, warehouseId: undefined }
      this.dateRange = []
      this.loadData()
    }
  }
}
</script>
