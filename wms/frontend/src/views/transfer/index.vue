<template>
  <div class="page-container">
    <div class="page-card">
      <div class="table-toolbar">
        <el-form :inline="true" :model="query" @submit.native.prevent="handleSearch">
          <el-form-item>
            <el-input v-model="query.keyword" placeholder="调拨单号" clearable style="width: 190px" />
          </el-form-item>
          <el-form-item>
            <el-select v-model="query.status" placeholder="单据状态" clearable style="width: 130px">
              <el-option label="草稿" :value="0" />
              <el-option label="已完成" :value="2" />
              <el-option label="已取消" :value="3" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-button type="primary" icon="el-icon-search" @click="handleSearch">查询</el-button>
            <el-button icon="el-icon-refresh" @click="handleReset">重置</el-button>
          </el-form-item>
        </el-form>
        <el-button v-permission="'transfer:edit'" type="primary" icon="el-icon-plus" @click="openCreate">新建调拨单</el-button>
      </div>

      <el-table v-loading="loading" :data="list" border stripe>
        <el-table-column prop="orderNo" label="调拨单号" width="180" />
        <el-table-column prop="fromWarehouseName" label="调出仓库" width="140" />
        <el-table-column prop="toWarehouseName" label="调入仓库" width="140" />
        <el-table-column prop="transferDate" label="调拨日期" width="120">
          <template slot-scope="{ row }">{{ formatDate(row.transferDate) }}</template>
        </el-table-column>
        <el-table-column prop="itemCount" label="明细数" width="90" align="center" />
        <el-table-column prop="totalQty" label="调拨总量" width="100" align="right" />
        <el-table-column prop="status" label="状态" width="100" align="center">
          <template slot-scope="{ row }">
            <el-tag :type="statusTagType(row.status)" size="mini">{{ statusName(row.status) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="createdByName" label="创建人" width="100" />
        <el-table-column label="操作" width="220" fixed="right">
          <template slot-scope="{ row }">
            <el-button type="text" @click="openDetail(row)">查看</el-button>
            <el-button v-if="row.status === 0" v-permission="'transfer:edit'" type="text" @click="openEdit(row)">编辑</el-button>
            <el-button v-if="row.status === 0" v-permission="'transfer:execute'" type="text" @click="handleComplete(row)">执行调拨</el-button>
            <el-button v-if="row.status === 0" v-permission="'transfer:edit'" type="text" class="text-danger" @click="handleDelete(row)">删除</el-button>
            <el-button v-if="row.status === 0" v-permission="'transfer:edit'" type="text" class="text-danger" @click="handleCancel(row)">取消</el-button>
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

    <el-dialog :title="form.id ? '编辑调拨单' : '新建调拨单'" :visible.sync="dialogVisible" width="960px">
      <el-form ref="form" :model="form" :rules="rules" label-width="90px">
        <el-row :gutter="16">
          <el-col :span="8">
            <el-form-item label="调出仓库" prop="fromWarehouseId">
              <el-select v-model="form.fromWarehouseId" style="width: 100%" @change="loadFromLocations">
                <el-option v-for="item in warehouses" :key="item.id" :label="item.name" :value="item.id" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="调入仓库" prop="toWarehouseId">
              <el-select v-model="form.toWarehouseId" style="width: 100%" @change="loadToLocations">
                <el-option v-for="item in warehouses" :key="item.id" :label="item.name" :value="item.id" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="调拨日期" prop="transferDate">
              <el-date-picker v-model="form.transferDate" type="date" value-format="yyyy-MM-dd" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="24">
            <el-form-item label="备注">
              <el-input v-model="form.remark" />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>

      <div class="table-toolbar">
        <span class="card-title">调拨明细</span>
        <el-button type="primary" plain icon="el-icon-plus" size="mini" @click="addItem">添加明细</el-button>
      </div>
      <el-table :data="form.items" border size="mini">
        <el-table-column label="物料" min-width="170">
          <template slot-scope="{ row }">
            <el-select v-model="row.materialId" filterable placeholder="选择物料" style="width: 100%">
              <el-option v-for="item in materials" :key="item.id" :label="`${item.code} ${item.name}`" :value="item.id" />
            </el-select>
          </template>
        </el-table-column>
        <el-table-column label="调出库位" min-width="150">
          <template slot-scope="{ row }">
            <el-select v-model="row.fromLocationId" filterable placeholder="选择库位" style="width: 100%">
              <el-option v-for="item in fromLocations" :key="item.id" :label="`${item.code} ${item.name}`" :value="item.id" />
            </el-select>
          </template>
        </el-table-column>
        <el-table-column label="调入库位" min-width="150">
          <template slot-scope="{ row }">
            <el-select v-model="row.toLocationId" filterable placeholder="选择库位" style="width: 100%">
              <el-option v-for="item in toLocations" :key="item.id" :label="`${item.code} ${item.name}`" :value="item.id" />
            </el-select>
          </template>
        </el-table-column>
        <el-table-column label="批次号" width="120">
          <template slot-scope="{ row }">
            <el-input v-model="row.batchNo" placeholder="可选" />
          </template>
        </el-table-column>
        <el-table-column label="调拨数量" width="120">
          <template slot-scope="{ row }">
            <el-input-number v-model="row.quantity" :min="0" :precision="2" :controls="false" style="width: 100%" />
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

    <el-drawer title="调拨单详情" :visible.sync="detailVisible" size="820px">
      <div v-if="detail" class="detail-body">
        <el-descriptions :column="3" border size="small">
          <el-descriptions-item label="调拨单号">{{ detail.orderNo }}</el-descriptions-item>
          <el-descriptions-item label="状态">{{ statusName(detail.status) }}</el-descriptions-item>
          <el-descriptions-item label="调拨日期">{{ formatDate(detail.transferDate) }}</el-descriptions-item>
          <el-descriptions-item label="调出仓库">{{ detail.fromWarehouseName }}</el-descriptions-item>
          <el-descriptions-item label="调入仓库">{{ detail.toWarehouseName }}</el-descriptions-item>
          <el-descriptions-item label="创建人">{{ detail.createdByName || '—' }}</el-descriptions-item>
          <el-descriptions-item label="备注" :span="3">{{ detail.remark || '—' }}</el-descriptions-item>
        </el-descriptions>

        <el-table :data="detail.items" border size="mini" style="margin-top: 16px">
          <el-table-column prop="materialCode" label="物料编码" width="120" />
          <el-table-column prop="materialName" label="物料名称" min-width="150" show-overflow-tooltip />
          <el-table-column prop="fromLocationCode" label="调出库位" width="110" />
          <el-table-column prop="toLocationCode" label="调入库位" width="110" />
          <el-table-column prop="batchNo" label="批次号" width="120" />
          <el-table-column prop="quantity" label="数量" width="100" align="right" />
        </el-table>
      </div>
    </el-drawer>
  </div>
</template>

<script>
import { transferApi } from '@/api/orders'
import { warehouseApi, locationApi, materialApi } from '@/api/masterdata'

const emptyForm = () => ({
  id: null,
  fromWarehouseId: undefined,
  toWarehouseId: undefined,
  transferDate: new Date().toISOString().substring(0, 10),
  remark: '',
  items: []
})

export default {
  name: 'Transfer',
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
      materials: [],
      fromLocations: [],
      toLocations: [],
      query: { page: 1, pageSize: 20, keyword: '', status: undefined },
      form: emptyForm(),
      rules: {
        fromWarehouseId: [{ required: true, message: '请选择调出仓库', trigger: 'change' }],
        toWarehouseId: [{ required: true, message: '请选择调入仓库', trigger: 'change' }],
        transferDate: [{ required: true, message: '请选择调拨日期', trigger: 'change' }]
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
    formatDate(value) {
      return value ? value.substring(0, 10) : '—'
    },
    async loadOptions() {
      const [warehouses, materials] = await Promise.all([warehouseApi.options(), materialApi.options()])
      this.warehouses = warehouses
      this.materials = materials
    },
    async loadFromLocations(warehouseId) {
      this.fromLocations = warehouseId ? await locationApi.options({ warehouseId }) : []
      this.form.items.forEach(item => {
        item.fromLocationId = undefined
      })
    },
    async loadToLocations(warehouseId) {
      this.toLocations = warehouseId ? await locationApi.options({ warehouseId }) : []
      this.form.items.forEach(item => {
        item.toLocationId = undefined
      })
    },
    async loadData() {
      this.loading = true
      try {
        const result = await transferApi.page(this.query)
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
      this.query = { page: 1, pageSize: 20, keyword: '', status: undefined }
      this.loadData()
    },
    addItem() {
      this.form.items.push({
        materialId: undefined,
        fromLocationId: undefined,
        toLocationId: undefined,
        batchNo: '',
        quantity: 1
      })
    },
    openCreate() {
      this.form = emptyForm()
      this.fromLocations = []
      this.toLocations = []
      this.dialogVisible = true
      this.$nextTick(() => this.$refs.form && this.$refs.form.clearValidate())
    },
    async openEdit(row) {
      const detail = await transferApi.get(row.id)
      this.form = {
        id: detail.id,
        fromWarehouseId: detail.fromWarehouseId,
        toWarehouseId: detail.toWarehouseId,
        transferDate: detail.transferDate.substring(0, 10),
        remark: detail.remark,
        items: detail.items.map(item => ({
          materialId: item.materialId,
          fromLocationId: item.fromLocationId,
          toLocationId: item.toLocationId,
          batchNo: item.batchNo,
          quantity: item.quantity
        }))
      }
      this.fromLocations = await locationApi.options({ warehouseId: detail.fromWarehouseId })
      this.toLocations = await locationApi.options({ warehouseId: detail.toWarehouseId })
      this.dialogVisible = true
      this.$nextTick(() => this.$refs.form && this.$refs.form.clearValidate())
    },
    async openDetail(row) {
      this.detail = await transferApi.get(row.id)
      this.detailVisible = true
    },
    submit() {
      this.$refs.form.validate(async valid => {
        if (!valid) return
        if (this.form.fromWarehouseId === this.form.toWarehouseId) {
          this.$message.warning('调出仓库与调入仓库不能相同')
          return
        }
        if (!this.form.items.length) {
          this.$message.warning('请至少添加一条调拨明细')
          return
        }
        const invalid = this.form.items.some(
          item => !item.materialId || !item.fromLocationId || !item.toLocationId || item.quantity <= 0
        )
        if (invalid) {
          this.$message.warning('请完善明细的物料、库位与数量')
          return
        }
        this.saving = true
        try {
          if (this.form.id) {
            await transferApi.update(this.form.id, this.form)
          } else {
            await transferApi.create(this.form)
          }
          this.$message.success('保存成功')
          this.dialogVisible = false
          this.loadData()
        } finally {
          this.saving = false
        }
      })
    },
    handleComplete(row) {
      this.$confirm('执行调拨将扣减调出库位并增加调入库位库存，确认？', '提示', { type: 'warning' })
        .then(async () => {
          await transferApi.complete(row.id)
          this.$message.success('调拨完成，库存已转移')
          this.loadData()
        })
        .catch(() => {})
    },
    handleCancel(row) {
      this.$confirm(`确认取消调拨单「${row.orderNo}」？`, '提示', { type: 'warning' })
        .then(async () => {
          await transferApi.cancel(row.id)
          this.$message.success('调拨单已取消')
          this.loadData()
        })
        .catch(() => {})
    },
    handleDelete(row) {
      this.$confirm(`确认删除调拨单「${row.orderNo}」？`, '提示', { type: 'warning' })
        .then(async () => {
          await transferApi.remove(row.id)
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
