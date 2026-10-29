<template>
  <div class="page-container">
    <div class="page-card">
      <div class="table-toolbar">
        <span class="text-muted">物料分类用于统计与筛选，支持两级结构</span>
        <el-button v-permission="'category:edit'" type="primary" icon="el-icon-plus" @click="openCreate">新增分类</el-button>
      </div>

      <el-table v-loading="loading" :data="list" border stripe row-key="id">
        <el-table-column prop="name" label="分类名称" min-width="180" />
        <el-table-column prop="parentName" label="上级分类" min-width="160">
          <template slot-scope="{ row }">{{ row.parentName || '顶级分类' }}</template>
        </el-table-column>
        <el-table-column prop="sort" label="排序" width="90" align="center" />
        <el-table-column prop="status" label="状态" width="90" align="center">
          <template slot-scope="{ row }">
            <el-tag :type="row.status === 1 ? 'success' : 'info'" size="mini">
              {{ row.status === 1 ? '启用' : '停用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="remark" label="备注" min-width="200" show-overflow-tooltip />
        <el-table-column label="操作" width="150">
          <template slot-scope="{ row }">
            <el-button v-permission="'category:edit'" type="text" @click="openEdit(row)">编辑</el-button>
            <el-button v-permission="'category:edit'" type="text" class="text-danger" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </div>

    <el-dialog :title="form.id ? '编辑分类' : '新增分类'" :visible.sync="dialogVisible" width="480px">
      <el-form ref="form" :model="form" :rules="rules" label-width="90px">
        <el-form-item label="分类名称" prop="name">
          <el-input v-model="form.name" />
        </el-form-item>
        <el-form-item label="上级分类">
          <el-select v-model="form.parentId" style="width: 100%">
            <el-option label="顶级分类" :value="0" />
            <el-option v-for="item in parentOptions" :key="item.id" :label="item.name" :value="item.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="排序">
          <el-input-number v-model="form.sort" :min="0" style="width: 100%" />
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
import { categoryApi } from '@/api/masterdata'

const emptyForm = () => ({ id: null, name: '', parentId: 0, sort: 0, status: 1, remark: '' })

export default {
  name: 'Categories',
  data() {
    return {
      loading: false,
      saving: false,
      dialogVisible: false,
      list: [],
      form: emptyForm(),
      rules: { name: [{ required: true, message: '请输入分类名称', trigger: 'blur' }] }
    }
  },
  computed: {
    parentOptions() {
      return this.list.filter(item => !this.form.id || item.id !== this.form.id)
    }
  },
  created() {
    this.loadData()
  },
  methods: {
    async loadData() {
      this.loading = true
      try {
        this.list = await categoryApi.list()
      } finally {
        this.loading = false
      }
    },
    openCreate() {
      this.form = emptyForm()
      this.dialogVisible = true
      this.$nextTick(() => this.$refs.form && this.$refs.form.clearValidate())
    },
    openEdit(row) {
      this.form = { id: row.id, name: row.name, parentId: row.parentId, sort: row.sort, status: row.status, remark: row.remark }
      this.dialogVisible = true
      this.$nextTick(() => this.$refs.form && this.$refs.form.clearValidate())
    },
    submit() {
      this.$refs.form.validate(async valid => {
        if (!valid) return
        this.saving = true
        try {
          if (this.form.id) {
            await categoryApi.update(this.form.id, this.form)
          } else {
            await categoryApi.create(this.form)
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
      this.$confirm(`确认删除分类「${row.name}」？`, '提示', { type: 'warning' })
        .then(async () => {
          await categoryApi.remove(row.id)
          this.$message.success('删除成功')
          this.loadData()
        })
        .catch(() => {})
    }
  }
}
</script>
