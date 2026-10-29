<template>
  <div class="page-container">
    <el-row :gutter="16">
      <el-col :xs="12" :sm="8" :md="6" v-for="card in cards" :key="card.label">
        <div class="page-card stat-card">
          <div class="stat-icon" :style="{ color: card.color }">
            <i :class="card.icon"></i>
          </div>
          <div>
            <div class="stat-label">{{ card.label }}</div>
            <div class="stat-value">{{ card.value }}</div>
          </div>
        </div>
      </el-col>
    </el-row>

    <el-row :gutter="16" class="chart-row">
      <el-col :md="15">
        <div class="page-card">
          <div class="card-title">近 7 天出入库趋势</div>
          <div ref="trendChart" class="chart"></div>
        </div>
      </el-col>
      <el-col :md="9">
        <div class="page-card">
          <div class="card-title">库存金额分类分布</div>
          <div ref="pieChart" class="chart"></div>
        </div>
      </el-col>
    </el-row>

    <div class="page-card">
      <div class="card-title">最近库存流水</div>
      <el-table :data="dashboard.recentTransactions" border stripe>
        <el-table-column prop="occurredAt" label="发生时间" min-width="150">
          <template slot-scope="{ row }">{{ formatTime(row.occurredAt) }}</template>
        </el-table-column>
        <el-table-column prop="typeName" label="类型" width="110">
          <template slot-scope="{ row }">{{ typeName(row.type) }}</template>
        </el-table-column>
        <el-table-column prop="materialCode" label="物料编码" width="130" />
        <el-table-column prop="materialName" label="物料名称" min-width="140" show-overflow-tooltip />
        <el-table-column prop="warehouseName" label="仓库" width="120" />
        <el-table-column prop="quantity" label="数量" width="100" align="right">
          <template slot-scope="{ row }">
            <span :class="row.quantity >= 0 ? 'text-success' : 'text-danger'">{{ row.quantity }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="balanceQty" label="结存" width="100" align="right" />
        <el-table-column prop="operatorName" label="操作人" width="110" />
      </el-table>
    </div>
  </div>
</template>

<script>
import * as echarts from 'echarts'
import { reportApi } from '@/api/inventory'

const TRANSACTION_TYPES = {
  1: '入库',
  2: '出库',
  3: '调拨入',
  4: '调拨出',
  5: '锁定',
  6: '释放锁定',
  7: '盘点调整',
  8: '期初'
}

export default {
  name: 'Dashboard',
  data() {
    return {
      dashboard: {
        trend: [],
        categoryDistribution: [],
        recentTransactions: []
      }
    }
  },
  computed: {
    cards() {
      const data = this.dashboard
      return [
        { label: '物料总数', value: data.materialCount || 0, icon: 'el-icon-box', color: '#409eff' },
        { label: '库存总量', value: this.formatNumber(data.stockQty), icon: 'el-icon-s-grid', color: '#67c23a' },
        { label: '库存金额（元）', value: this.formatNumber(data.stockAmount), icon: 'el-icon-money', color: '#e6a23c' },
        { label: '库存预警', value: data.alertCount || 0, icon: 'el-icon-warning-outline', color: '#f56c6c' },
        { label: '今日入库单', value: data.todayInboundOrders || 0, icon: 'el-icon-download', color: '#409eff' },
        { label: '今日出库单', value: data.todayOutboundOrders || 0, icon: 'el-icon-upload2', color: '#67c23a' },
        { label: '待处理入库', value: data.pendingInbound || 0, icon: 'el-icon-time', color: '#e6a23c' },
        { label: '待处理出库', value: data.pendingOutbound || 0, icon: 'el-icon-time', color: '#909399' }
      ]
    }
  },
  mounted() {
    this.loadData()
    window.addEventListener('resize', this.resizeCharts)
  },
  beforeDestroy() {
    window.removeEventListener('resize', this.resizeCharts)
    this.trendChart && this.trendChart.dispose()
    this.pieChart && this.pieChart.dispose()
  },
  methods: {
    async loadData() {
      this.dashboard = await reportApi.dashboard()
      this.$nextTick(() => {
        this.renderTrend()
        this.renderPie()
      })
    },
    renderTrend() {
      if (!this.$refs.trendChart) return
      this.trendChart = this.trendChart || echarts.init(this.$refs.trendChart)
      const trend = this.dashboard.trend || []
      this.trendChart.setOption({
        tooltip: { trigger: 'axis' },
        legend: { data: ['入库数量', '出库数量'] },
        grid: { left: 40, right: 20, top: 40, bottom: 30 },
        xAxis: { type: 'category', data: trend.map(x => x.date) },
        yAxis: { type: 'value' },
        series: [
          { name: '入库数量', type: 'line', smooth: true, data: trend.map(x => x.inboundQty) },
          { name: '出库数量', type: 'line', smooth: true, data: trend.map(x => x.outboundQty) }
        ]
      })
    },
    renderPie() {
      if (!this.$refs.pieChart) return
      this.pieChart = this.pieChart || echarts.init(this.$refs.pieChart)
      const items = this.dashboard.categoryDistribution || []
      this.pieChart.setOption({
        tooltip: { trigger: 'item', formatter: '{b}: {c} 元 ({d}%)' },
        series: [
          {
            name: '库存金额',
            type: 'pie',
            radius: ['40%', '65%'],
            data: items.length ? items : [{ name: '暂无库存', value: 1 }]
          }
        ]
      })
    },
    resizeCharts() {
      this.trendChart && this.trendChart.resize()
      this.pieChart && this.pieChart.resize()
    },
    formatNumber(value) {
      const number = Number(value || 0)
      return number.toLocaleString('zh-CN', { maximumFractionDigits: 2 })
    },
    formatTime(value) {
      if (!value) return '—'
      return value.replace('T', ' ').substring(0, 19)
    },
    typeName(type) {
      return TRANSACTION_TYPES[type] || '其他'
    }
  }
}
</script>

<style scoped>
.stat-card {
  display: flex;
  align-items: center;
  margin-bottom: 16px;
}

.stat-icon {
  font-size: 30px;
  margin-right: 14px;
}

.stat-label {
  color: #909399;
  font-size: 12px;
}

.stat-value {
  font-size: 20px;
  font-weight: 600;
  margin-top: 4px;
}

.chart-row {
  margin-bottom: 16px;
}

.card-title {
  font-size: 14px;
  font-weight: 600;
  margin-bottom: 12px;
}

.chart {
  height: 300px;
}
</style>
