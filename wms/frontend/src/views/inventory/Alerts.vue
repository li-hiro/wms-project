<template>
  <div class="page-container">
    <div class="page-card">
      <div class="table-toolbar">
        <span class="text-muted">系统按「物料 + 仓库」汇总库存数量，低于安全库存时进入预警列表</span>
        <el-button icon="el-icon-refresh" @click="loadData">刷新</el-button>
      </div>

      <el-table v-loading="loading" :data="list" border stripe>
        <el-table-column prop="materialCode" label="物料编码" width="140" />
        <el-table-column prop="materialName" label="物料名称" min-width="170" show-overflow-tooltip />
        <el-table-column prop="spec" label="规格" min-width="140" show-overflow-tooltip />
        <el-table-column prop="warehouseName" label="仓库" width="130" />
        <el-table-column prop="onHandQty" label="在库数量" width="110" align="right" />
        <el-table-column prop="lockedQty" label="锁定数量" width="110" align="right" />
        <el-table-column prop="safetyStock" label="安全库存" width="110" align="right" />
        <el-table-column prop="shortageQty" label="缺口数量" width="110" align="right">
          <template slot-scope="{ row }">
            <span class="text-danger">{{ row.shortageQty }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="unit" label="单位" width="70" align="center" />
      </el-table>

      <div class="empty-tip" v-if="!loading && !list.length">当前没有低于安全库存的物料</div>
    </div>
  </div>
</template>

<script>
import { inventoryApi } from '@/api/inventory'

export default {
  name: 'InventoryAlerts',
  data() {
    return {
      loading: false,
      list: []
    }
  },
  created() {
    this.loadData()
  },
  methods: {
    async loadData() {
      this.loading = true
      try {
        this.list = await inventoryApi.alerts()
      } finally {
        this.loading = false
      }
    }
  }
}
</script>

<style scoped>
.empty-tip {
  padding: 24px 0;
  text-align: center;
  color: #909399;
}
</style>
