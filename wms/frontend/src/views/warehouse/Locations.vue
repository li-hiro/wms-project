<template>
  <div class="page-container">
    <div class="page-card">
      <div class="table-toolbar">
        <el-form :inline="true" :model="query" @submit.native.prevent="handleSearch">
          <el-form-item>
            <el-select v-model="query.warehouseId" placeholder="所属仓库" clearable style="width: 180px">
              <el-option v-for="item in warehouses" :key="item.id" :label="item.name" :value="item.id" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-input v-model="query.keyword" placeholder="库位编码 / 名称" clearable style="width: 200px" />
          </el-form-item>
          <el-form-item>
            <el-button type="primary" icon="el-icon-search" @click="handleSearch">查询</el-button>
            <el-button icon="el-icon-refresh" @click="handleReset">重置</el-button>
          </el-form-item>
        </el-form>
        <el-button v-permission="'location:edit'" type="primary" icon="el-icon-plus" @click="openCreate">新增库位</el-button>
      </div>

      <el-table v-loading="loading" :data="list" border stripe>
        <el-table-column prop="warehouseName" label="所属仓库" width="140" />
        <el-table-column prop="code" label="库位编码" width="140" />
        <el-table-column prop="name" label="库位名称" min-width="140" show-overflow-tooltip />
        <el-table-column prop="zone" label="库区" width="110" />
        <el-table-column prop="type" label="类型" width="100" align="center">
          <template slot-scope="{ row }">{{ typeName(row.type) }}</template>
        </el-table-column>
        <el-table-column prop="capacity" label="容量" width="100" align="right" />
        <el-table-column prop="onHandQty" label="当前库存" width="100" align="right" />
        <el-table-column prop="status" label="状态" width="90" align="center">
          <template slot-scope="{ row }">
            <el-tag :type="row.status === 1 ? 'success' : 'info'" size="mini">
              {{ row.status === 1 ? '启用' : '停用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="150" fixed="right">
          <template slot-scope="{ row }">
            <el-button v-permission="'location:edit'" type="text" @click="openEdit(row)">编辑</el-button>
            <el-button v-permission="'location:edit'" type="text" class="text-danger" @click="handleDelete(row)">删除</el-button>
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

    <el-dialog :title="form.id ? '编辑库位' : '新增库位'" :visible.sync="dialogVisible" width="560px">
      <el-form ref="form" :model="form" :rules="rules" label-width="90px">
        <el-form-item label="所属仓库" prop="warehouseId">
          <el-select v-model="form.warehouseId" placeholder="请选择仓库" style="width: 100%">
            <el-option v-for="item in warehouses" :key="item.id" :label="item.name" :value="item.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="库位编码" prop="code">
          <el-input v-model="form.code" placeholder="如 A-01-01" />
        </el-form-item>
        <el-form-item label="库位名称" prop="name">
          <el-input v-model="form.name" />
        </el-form-item>
        <el-form-item label="库区">
          <el-input v-model="form.zone" placeholder="如 A 区" />
        </el-form-item>
        <el-form-item label="库位类型">
          <el-select v-model="form.type" style="width: 100%">
            <el-option label="存储位" :value="1" />
            <el-option label="收货位" :value="2" />
            <el-option label="发货位" :value="3" />
            <el-option label="其他" :value="9" />
          </el-select>
        </el-form-item>
        <el-form-item label="容量">
          <el-input-number v-model="form.capacity" :min="0" :precision="2" style="width: 100%" />
        </el-form-item>
        <el-form-item label="状态">
          <el-switch v-model="form.status" :active-value="1" :inactive-value="0" active-text="启用" inactive-text="停用" />
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="form.remark" type="textarea" :rows="2" />
        </el-form-item>
      </el-form>
      <span slot="footer">
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="submit">保存</el-button>
      </span>
    </el-dialog>
  </div>
</template>

<script>
import { locationApi, warehouseApi } from '@/api/masterdata'

const emptyForm = () => ({
  id: null,
  warehouseId: undefined,
  code: '',
  name: '',
  zone: '',
  type: 1,
  capacity: 0,
  status: 1,
  remark: ''
})

export default {
  name: 'Locations',
  data() {
    return {
      loading: false,
      saving: false,
      dialogVisible: false,
      list: [],
      total: 0,
      warehouses: [],
      query: { page: 1, pageSize: 20, keyword: '', warehouseId: undefined },
      form: emptyForm(),
      rules: {
        warehouseId: [{ required: true, message: '请选择所属仓库', trigger: 'change' }],
        code: [{ required: true, message: '请输入库位编码', trigger: 'blur' }],
        name: [{ required: true, message: '请输入库位名称', trigger: 'blur' }]
      }
    }
  },
  created() {
    this.loadWarehouses()
    this.loadData()
  },
  methods: {
    typeName(type) {
      return { 1: '存储位', 2: '收货位', 3: '发货位', 9: '其他' }[type] || '其他'
    },
    async loadWarehouses() {
      this.warehouses = await warehouseApi.options()
    },
    async loadData() {
      this.loading = true
      try {
        const result = await locationApi.page(this.query)
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
      this.query = { page: 1, pageSize: 20, keyword: '', warehouseId: undefined }
      this.loadData()
    },
    openCreate() {
      this.form = emptyForm()
      this.dialogVisible = true
      this.$nextTick(() => this.$refs.form && this.$refs.form.clearValidate())
    },
    openEdit(row) {
      this.form = { ...row }
      this.dialogVisible = true
      this.$nextTick(() => this.$refs.form && this.$refs.form.clearValidate())
    },
    submit() {
      this.$refs.form.validate(async valid => {
        if (!valid) return
        this.saving = true
        try {
          if (this.form.id) {
            await locationApi.update(this.form.id, this.form)
          } else {
            await locationApi.create(this.form)
          }
          this.$message.success('保存成功')
          this.dialogVisible = false
          this.loadData()
        } finally {
          this.saving = false
        }
      })
    },
    handleDelete(row) {
      this.$confirm(`确认删除库位「${row.code}」？`, '提示', { type: 'warning' })
        .then(async () => {
          await locationApi.remove(row.id)
          this.$message.success('删除成功')
          this.loadData()
        })
        .catch(() => {})
    }
  }
}
</script>
