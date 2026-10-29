<template>
  <div class="page-container">
    <div class="page-card">
      <div class="table-toolbar">
        <el-form :inline="true" :model="query" @submit.native.prevent="handleSearch">
          <el-form-item>
            <el-input v-model="query.keyword" placeholder="账号 / 昵称" clearable style="width: 200px" />
          </el-form-item>
          <el-form-item>
            <el-select v-model="query.roleId" placeholder="角色" clearable style="width: 150px">
              <el-option v-for="item in roles" :key="item.id" :label="item.name" :value="item.id" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-select v-model="query.status" placeholder="状态" clearable style="width: 120px">
              <el-option label="启用" :value="1" />
              <el-option label="停用" :value="0" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-button type="primary" icon="el-icon-search" @click="handleSearch">查询</el-button>
            <el-button icon="el-icon-refresh" @click="handleReset">重置</el-button>
          </el-form-item>
        </el-form>
        <el-button v-permission="'system:user:edit'" type="primary" icon="el-icon-plus" @click="openCreate">新增用户</el-button>
      </div>

      <el-table v-loading="loading" :data="list" border stripe>
        <el-table-column prop="userName" label="登录账号" width="140" />
        <el-table-column prop="nickName" label="昵称" width="140" />
        <el-table-column label="角色" min-width="180">
          <template slot-scope="{ row }">
            <el-tag v-for="name in row.roleNames" :key="name" size="mini" style="margin-right: 4px">{{ name }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="email" label="邮箱" min-width="180" show-overflow-tooltip />
        <el-table-column prop="phone" label="手机号" width="130" />
        <el-table-column prop="lastLoginAt" label="最近登录" width="160">
          <template slot-scope="{ row }">{{ formatDateTime(row.lastLoginAt) }}</template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="90" align="center">
          <template slot-scope="{ row }">
            <el-tag :type="row.status === 1 ? 'success' : 'info'" size="mini">
              {{ row.status === 1 ? '启用' : '停用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="260" fixed="right">
          <template slot-scope="{ row }">
            <el-button v-permission="'system:user:edit'" type="text" @click="openEdit(row)">编辑</el-button>
            <el-button v-permission="'system:user:edit'" type="text" @click="handleResetPassword(row)">重置密码</el-button>
            <el-button v-permission="'system:user:edit'" type="text" @click="handleToggle(row)">
              {{ row.status === 1 ? '停用' : '启用' }}
            </el-button>
            <el-button v-permission="'system:user:edit'" type="text" class="text-danger" @click="handleDelete(row)">删除</el-button>
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

    <el-dialog :title="form.id ? '编辑用户' : '新增用户'" :visible.sync="dialogVisible" width="560px">
      <el-form ref="form" :model="form" :rules="rules" label-width="90px">
        <el-form-item label="登录账号" prop="userName">
          <el-input v-model="form.userName" :disabled="!!form.id" />
        </el-form-item>
        <el-form-item label="昵称" prop="nickName">
          <el-input v-model="form.nickName" />
        </el-form-item>
        <el-form-item v-if="!form.id" label="初始密码" prop="password">
          <el-input v-model="form.password" type="password" show-password placeholder="至少 6 位" />
        </el-form-item>
        <el-form-item label="所属角色" prop="roleIds">
          <el-select v-model="form.roleIds" multiple style="width: 100%">
            <el-option v-for="item in roles" :key="item.id" :label="item.name" :value="item.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="邮箱">
          <el-input v-model="form.email" />
        </el-form-item>
        <el-form-item label="手机号">
          <el-input v-model="form.phone" />
        </el-form-item>
        <el-form-item label="状态">
          <el-switch v-model="form.status" :active-value="1" :inactive-value="0" active-text="启用" inactive-text="停用" />
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
import { userApi, roleApi } from '@/api/system'

const emptyForm = () => ({
  id: null,
  userName: '',
  nickName: '',
  password: '',
  email: '',
  phone: '',
  status: 1,
  roleIds: []
})

export default {
  name: 'SystemUsers',
  data() {
    return {
      loading: false,
      saving: false,
      dialogVisible: false,
      list: [],
      total: 0,
      roles: [],
      query: { page: 1, pageSize: 20, keyword: '', status: undefined, roleId: undefined },
      form: emptyForm(),
      rules: {
        userName: [{ required: true, message: '请输入登录账号', trigger: 'blur' }],
        nickName: [{ required: true, message: '请输入昵称', trigger: 'blur' }],
        password: [
          { required: true, message: '请输入初始密码', trigger: 'blur' },
          { min: 6, message: '密码长度不能少于 6 位', trigger: 'blur' }
        ],
        roleIds: [{ type: 'array', required: true, message: '请至少选择一个角色', trigger: 'change' }]
      }
    }
  },
  created() {
    this.loadRoles()
    this.loadData()
  },
  methods: {
    formatDateTime(value) {
      return value ? value.replace('T', ' ').substring(0, 19) : '—'
    },
    async loadRoles() {
      this.roles = await roleApi.list()
    },
    async loadData() {
      this.loading = true
      try {
        const result = await userApi.page(this.query)
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
      this.query = { page: 1, pageSize: 20, keyword: '', status: undefined, roleId: undefined }
      this.loadData()
    },
    openCreate() {
      this.form = emptyForm()
      this.dialogVisible = true
      this.$nextTick(() => this.$refs.form && this.$refs.form.clearValidate())
    },
    openEdit(row) {
      this.form = {
        id: row.id,
        userName: row.userName,
        nickName: row.nickName,
        email: row.email,
        phone: row.phone,
        status: row.status,
        roleIds: row.roleIds || []
      }
      this.dialogVisible = true
      this.$nextTick(() => this.$refs.form && this.$refs.form.clearValidate())
    },
    submit() {
      this.$refs.form.validate(async valid => {
        if (!valid) return
        this.saving = true
        try {
          const payload = { ...this.form }
          if (payload.id) delete payload.password
          if (payload.id) {
            await userApi.update(payload.id, payload)
          } else {
            await userApi.create(payload)
          }
          this.$message.success('保存成功')
          this.dialogVisible = false
          this.loadData()
        } finally {
          this.saving = false
        }
      })
    },
    handleResetPassword(row) {
      this.$prompt('请输入新密码（留空则由系统生成随机密码）', `重置「${row.userName}」的密码`, {
        inputPlaceholder: '新密码，可留空'
      })
        .then(async ({ value }) => {
          const password = await userApi.resetPassword(row.id, { password: value || null })
          this.$alert(`新密码：${password}`, '密码已重置', { type: 'success' })
        })
        .catch(() => {})
    },
    handleToggle(row) {
      const next = row.status === 1 ? 0 : 1
      this.$confirm(`确认${next === 1 ? '启用' : '停用'}用户「${row.userName}」？`, '提示', { type: 'warning' })
        .then(async () => {
          await userApi.toggleStatus(row.id, next)
          this.$message.success('状态已更新')
          this.loadData()
        })
        .catch(() => {})
    },
    handleDelete(row) {
      this.$confirm(`确认删除用户「${row.userName}」？`, '提示', { type: 'warning' })
        .then(async () => {
          await userApi.remove(row.id)
          this.$message.success('删除成功')
          this.loadData()
        })
        .catch(() => {})
    }
  }
}
</script>
