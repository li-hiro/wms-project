<template>
  <div class="page-container">
    <div class="page-card">
      <div class="table-toolbar">
        <el-form :inline="true" :model="query" @submit.native.prevent="handleSearch">
          <el-form-item>
            <el-input v-model="query.keyword" placeholder="出库单号" clearable style="width: 190px" />
          </el-form-item>
          <el-form-item>
            <el-select v-model="query.status" placeholder="单据状态" clearable style="width: 130px">
              <el-option v-for="item in statusOptions" :key="item.value" :label="item.label" :value="item.value" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-select v-model="query.warehouseId" placeholder="出库仓库" clearable style="width: 150px">
              <el-option v-for="item in warehouses" :key="item.id" :label="item.name" :value="item.id" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-button type="primary" icon="el-icon-search" @click="handleSearch">查询</el-button>
            <el-button icon="el-icon-refresh" @click="handleReset">重置</el-button>
          </el-form-item>
        </el-form>
        <el-button v-permission="'outbound:edit'" type="primary" icon="el-icon-plus" @click="openCreate">新建出库单</el-button>
      </div>

      <el-table v-loading="loading" :data="list" border stripe>
        <el-table-column prop="orderNo" label="出库单号" width="180" />
        <el-table-column prop="type" label="出库类型" width="110">
          <template slot-scope="{ row }">{{ outboundTypeName(row.type) }}</template>
        </el-table-column>
        <el-table-column prop="customerName" label="客户" min-width="150" show-overflow-tooltip />
        <el-table-column prop="warehouseName" label="出库仓库" width="130" />
        <el-table-column prop="plannedDate" label="计划日期" width="120">
          <template slot-scope="{ row }">{{ formatDate(row.plannedDate) }}</template>
        </el-table-column>
        <el-table-column prop="itemCount" label="明细数" width="80" align="center" />
        <el-table-column prop="totalQty" label="计划总量" width="100" align="right" />
        <el-table-column prop="status" label="状态" width="100" align="center">
          <template slot-scope="{ row }">
            <el-tag :type="statusTagType(row.status)" size="mini">{{ statusName(row.status) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="250" fixed="right">
          <template slot-scope="{ row }">
            <el-button type="text" @click="openDetail(row)">查看</el-button>
            <el-button v-if="row.status === 0" v-permission="'outbound:edit'" type="text" @click="openEdit(row)">编辑</el-button>
            <el-button v-if="row.status === 0" v-permission="'outbound:edit'" type="text" @click="handleConfirm(row)">确认</el-button>
            <el-button v-if="row.status === 1" v-permission="'outbound:execute'" type="text" @click="handleComplete(row)">执行出库</el-button>
            <el-button v-if="row.status === 0" v-permission="'outbound:edit'" type="text" class="text-danger" @click="handleDelete(row)">删除</el-button>
            <el-button
              v-if="row.status === 0 || row.status === 1"
              v-permission="'outbound:edit'"
              type="text"
              class="text-danger"
              @click="handleCancel(row)"
            >取消</el-button>
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

    <el-dialog :title="form.id ? '编辑出库单' : '新建出库单'" :visible.sync="dialogVisible" width="920px">
      <el-form ref="form" :model="form" :rules="rules" label-width="90px">
        <el-row :gutter="16">
          <el-col :span="8">
            <el-form-item label="出库类型" prop="type">
              <el-select v-model="form.type" style="width: 100%">
                <el-option label="销售出库" :value="1" />
                <el-option label="生产领料" :value="2" />
                <el-option label="其他出库" :value="9" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="客户">
              <el-select v-model="form.customerId" clearable style="width: 100%">
                <el-option v-for="item in customers" :key="item.id" :label="item.name" :value="item.id" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="出库仓库" prop="warehouseId">
              <el-select v-model="form.warehouseId" style="width: 100%" @change="handleWarehouseChange">
                <el-option v-for="item in warehouses" :key="item.id" :label="item.name" :value="item.id" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="计划日期" prop="plannedDate">
              <el-date-picker v-model="form.plannedDate" type="date" value-format="yyyy-MM-dd" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="16">
            <el-form-item label="备注">
              <el-input v-model="form.remark" />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>

      <div class="table-toolbar">
        <span class="card-title">出库明细</span>
        <el-button type="primary" plain icon="el-icon-plus" size="mini" @click="addItem">添加明细</el-button>
      </div>
      <el-table :data="form.items" border size="mini">
        <el-table-column label="物料" min-width="180">
          <template slot-scope="{ row }">
            <el-select v-model="row.materialId" filterable placeholder="选择物料" style="width: 100%">
              <el-option v-for="item in materials" :key="item.id" :label="`${item.code} ${item.name}`" :value="item.id" />
            </el-select>
          </template>
        </el-table-column>
        <el-table-column label="库位" min-width="160">
          <template slot-scope="{ row }">
            <el-select v-model="row.locationId" filterable clearable placeholder="选择库位" style="width: 100%">
              <el-option v-for="item in locations" :key="item.id" :label="`${item.code} ${item.name}`" :value="item.id" />
            </el-select>
          </template>
        </el-table-column>
        <el-table-column label="批次号" width="130">
          <template slot-scope="{ row }">
            <el-input v-model="row.batchNo" placeholder="可选" />
          </template>
        </el-table-column>
        <el-table-column label="申请数量" width="130">
          <template slot-scope="{ row }">
            <el-input-number v-model="row.requestedQty" :min="0" :precision="2" :controls="false" style="width: 100%" />
          </template>
        </el-table-column>
        <el-table-column label="单价" width="110">
          <template slot-scope="{ row }">
            <el-input-number v-model="row.price" :min="0" :precision="2" :controls="false" style="width: 100%" />
          </template>
        </el-table-column>
        <el-table-column label="备注" min-width="140">
          <template slot-scope="{ row }">
            <el-input v-model="row.remark" />
          </template>
        </el-table-column>
        <el-table-column label="操作" width="70" align="center">
          <template slot-scope="{ $index }">
            <el-button type="text" class="text-danger" @click="form.items.splice($index, 1)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>

      <span slot="footer">
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="submit">保存</el-button>
      </span>
    </el-dialog>

    <el-drawer title="出库单详情" :visible.sync="detailVisible" size="760px">
      <div v-if="detail" class="detail-body">
        <el-descriptions :column="2" border size="small">
          <el-descriptions-item label="出库单号">{{ detail.orderNo }}</el-descriptions-item>
          <el-descriptions-item label="状态">{{ statusName(detail.status) }}</el-descriptions-item>
          <el-descriptions-item label="出库类型">{{ outboundTypeName(detail.type) }}</el-descriptions-item>
          <el-descriptions-item label="客户">{{ detail.customerName || '—' }}</el-descriptions-item>
          <el-descriptions-item label="出库仓库">{{ detail.warehouseName }}</el-descriptions-item>
          <el-descriptions-item label="计划日期">{{ formatDate(detail.plannedDate) }}</el-descriptions-item>
          <el-descriptions-item label="创建人">{{ detail.createdByName || '—' }}</el-descriptions-item>
          <el-descriptions-item label="创建时间">{{ formatDateTime(detail.createdAt) }}</el-descriptions-item>
          <el-descriptions-item label="备注" :span="2">{{ detail.remark || '—' }}</el-descriptions-item>
        </el-descriptions>

        <el-table :data="detail.items" border size="mini" style="margin-top: 16px">
          <el-table-column prop="materialCode" label="物料编码" width="120" />
          <el-table-column prop="materialName" label="物料名称" min-width="150" show-overflow-tooltip />
          <el-table-column prop="locationCode" label="库位" width="110" />
          <el-table-column prop="batchNo" label="批次号" width="120" />
          <el-table-column prop="requestedQty" label="申请数量" width="100" align="right" />
          <el-table-column prop="shippedQty" label="实发数量" width="100" align="right" />
          <el-table-column prop="price" label="单价" width="90" align="right" />
        </el-table>
      </div>
    </el-drawer>
  </div>
</template>

<script>
import { outboundApi } from '@/api/orders'
import { warehouseApi, locationApi, customerApi, materialApi } from '@/api/masterdata'

const emptyForm = () => ({
  id: null,
  type: 1,
  customerId: undefined,
  warehouseId: undefined,
  plannedDate: new Date().toISOString().substring(0, 10),
  remark: '',
  items: []
})

export default {
  name: 'Outbound',
  data() {
    return {
      loading: false,
      saving: false,
      dialogVisible: false,
      detailVisible: false,
      detail: null,
      list: [],
      total: 0,
      warehouses: [],
      customers: [],
      materials: [],
      locations: [],
      query: { page: 1, pageSize: 20, keyword: '', status: undefined, warehouseId: undefined },
      form: emptyForm(),
      statusOptions: [
        { label: '草稿', value: 0 },
        { label: '已确认（已锁库）', value: 1 },
        { label: '已完成', value: 2 },
        { label: '已取消', value: 3 }
      ],
      rules: {
        type: [{ required: true, message: '请选择出库类型', trigger: 'change' }],
        warehouseId: [{ required: true, message: '请选择出库仓库', trigger: 'change' }],
        plannedDate: [{ required: true, message: '请选择计划日期', trigger: 'change' }]
      }
    }
  },
  created() {
    this.loadOptions()
    this.loadData()
  },
  methods: {
    statusName(status) {
      return { 0: '草稿', 1: '已确认', 2: '已完成', 3: '已取消' }[status] || '未知'
    },
    statusTagType(status) {
      return { 0: 'info', 1: 'warning', 2: 'success', 3: 'danger' }[status] || 'info'
    },
    outboundTypeName(type) {
      return { 1: '销售出库', 2: '生产领料', 9: '其他出库' }[type] || '其他'
    },
    formatDate(value) {
      return value ? value.substring(0, 10) : '—'
    },
    formatDateTime(value) {
      return value ? value.replace('T', ' ').substring(0, 19) : '—'
    },
    async loadOptions() {
      const [warehouses, customers, materials] = await Promise.all([
        warehouseApi.options(),
        customerApi.options(),
        materialApi.options()
      ])
      this.warehouses = warehouses
      this.customers = customers
      this.materials = materials
    },
    async loadLocations(warehouseId) {
      this.locations = warehouseId ? await locationApi.options({ warehouseId }) : []
    },
    async loadData() {
      this.loading = true
      try {
        const result = await outboundApi.page(this.query)
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
    handleWarehouseChange(warehouseId) {
      this.form.items.forEach(item => {
        item.locationId = undefined
      })
      this.loadLocations(warehouseId)
    },
    addItem() {
      this.form.items.push({ materialId: undefined, locationId: undefined, batchNo: '', requestedQty: 1, price: 0, remark: '' })
    },
    openCreate() {
      this.form = emptyForm()
      this.locations = []
      this.dialogVisible = true
      this.$nextTick(() => this.$refs.form && this.$refs.form.clearValidate())
    },
    async openEdit(row) {
      const detail = await outboundApi.get(row.id)
      this.form = {
        id: detail.id,
        type: detail.type,
        customerId: detail.customerId,
        warehouseId: detail.warehouseId,
        plannedDate: detail.plannedDate.substring(0, 10),
        remark: detail.remark,
        items: detail.items.map(item => ({
          materialId: item.materialId,
          locationId: item.locationId,
          batchNo: item.batchNo,
          requestedQty: item.requestedQty,
          price: item.price,
          remark: item.remark
        }))
      }
      await this.loadLocations(detail.warehouseId)
      this.dialogVisible = true
      this.$nextTick(() => this.$refs.form && this.$refs.form.clearValidate())
    },
    async openDetail(row) {
      this.detail = await outboundApi.get(row.id)
      this.detailVisible = true
    },
    submit() {
      this.$refs.form.validate(async valid => {
        if (!valid) return
        if (!this.form.items.length) {
          this.$message.warning('请至少添加一条出库明细')
          return
        }
        const invalid = this.form.items.some(item => !item.materialId || item.requestedQty <= 0)
        if (invalid) {
          this.$message.warning('请完善明细的物料与申请数量')
          return
        }
        this.saving = true
        try {
          if (this.form.id) {
            await outboundApi.update(this.form.id, this.form)
          } else {
            await outboundApi.create(this.form)
          }
          this.$message.success('保存成功')
          this.dialogVisible = false
          this.loadData()
        } finally {
          this.saving = false
        }
      })
    },
    handleConfirm(row) {
      this.$confirm(`确认出库单「${row.orderNo}」并锁定库存？`, '提示', { type: 'warning' })
        .then(async () => {
          await outboundApi.confirm(row.id)
          this.$message.success('出库单已确认，库存已锁定')
          this.loadData()
        })
        .catch(() => {})
    },
    handleComplete(row) {
      this.$confirm('执行出库将扣减库存，确认？', '提示', { type: 'warning' })
        .then(async () => {
          await outboundApi.complete(row.id)
          this.$message.success('出库完成，库存已扣减')
          this.loadData()
        })
        .catch(() => {})
    },
    handleCancel(row) {
      this.$confirm(`确认取消出库单「${row.orderNo}」？已锁定的库存会自动释放`, '提示', { type: 'warning' })
        .then(async () => {
          await outboundApi.cancel(row.id)
          this.$message.success('出库单已取消')
          this.loadData()
        })
        .catch(() => {})
    },
    handleDelete(row) {
      this.$confirm(`确认删除出库单「${row.orderNo}」？`, '提示', { type: 'warning' })
        .then(async () => {
          await outboundApi.remove(row.id)
          this.$message.success('删除成功')
          this.loadData()
        })
        .catch(() => {})
    }
  }
}
</script>

<style scoped>
.card-title {
  font-size: 14px;
  font-weight: 600;
}
.detail-body {
  padding: 0 20px 20px;
}
</style>
