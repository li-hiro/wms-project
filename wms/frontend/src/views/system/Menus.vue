<template>
  <div class="page-container">
    <div class="page-card">
      <div class="table-toolbar">
        <span class="text-muted">菜单用于控制侧边栏导航与页面访问权限，目录类型可包含子菜单</span>
        <el-button v-permission="'system:menu:edit'" type="primary" icon="el-icon-plus" @click="openCreate">新增菜单</el-button>
      </div>

      <el-table v-loading="loading" :data="tree" border row-key="id" :tree-props="{ children: 'children' }">
        <el-table-column prop="name" label="菜单名称" min-width="180" />
        <el-table-column prop="type" label="类型" width="90" align="center">
          <template slot-scope="{ row }">{{ row.type === 0 ? '目录' : '菜单' }}</template>
        </el-table-column>
        <el-table-column prop="path" label="路由路径" min-width="180" />
        <el-table-column prop="component" label="组件" min-width="180" show-overflow-tooltip />
        <el-table-column prop="permissionCode" label="权限编码" min-width="160" />
        <el-table-column prop="icon" label="图标" width="150">
          <template slot-scope="{ row }">
            <i v-if="row.icon" :class="row.icon"></i>
            <span class="text-muted">{{ row.icon }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="sort" label="排序" width="80" align="center" />
        <el-table-column prop="visible" label="显示" width="80" align="center">
          <template slot-scope="{ row }">
            <el-tag :type="row.visible ? 'success' : 'info'" size="mini">{{ row.visible ? '显示' : '隐藏' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="180" fixed="right">
          <template slot-scope="{ row }">
            <el-button v-permission="'system:menu:edit'" type="text" @click="openEdit(row)">编辑</el-button>
            <el-button v-permission="'system:menu:edit'" type="text" class="text-danger" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </div>

    <el-dialog :title="form.id ? '编辑菜单' : '新增菜单'" :visible.sync="dialogVisible" width="600px">
      <el-form ref="form" :model="form" :rules="rules" label-width="90px">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="菜单名称" prop="name">
              <el-input v-model="form.name" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="菜单类型" prop="type">
              <el-radio-group v-model="form.type">
                <el-radio :label="0">目录</el-radio>
                <el-radio :label="1">菜单</el-radio>
              </el-radio-group>
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="上级菜单">
          <el-select v-model="form.parentId" style="width: 100%">
            <el-option label="顶级菜单" :value="0" />
            <el-option v-for="item in parentOptions" :key="item.id" :label="item.name" :value="item.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="路由路径" prop="path">
          <el-input v-model="form.path" placeholder="如 /master/materials" />
        </el-form-item>
        <el-form-item label="组件路径">
          <el-input v-model="form.component" placeholder="如 master/Materials" />
        </el-form-item>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="权限编码">
              <el-input v-model="form.permissionCode" placeholder="如 material:view" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="图标">
              <el-input v-model="form.icon" placeholder="如 el-icon-box" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="排序">
              <el-input-number v-model="form.sort" :min="0" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="是否显示">
              <el-switch v-model="form.visible" active-text="显示" inactive-text="隐藏" />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
      <span slot="footer">
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="submit">保存</el-button>
      </span>
    </el-dialog>
  </div>
</template>

<script>
import { menuApi } from '@/api/system'

const emptyForm = () => ({
  id: null,
  name: '',
  path: '',
  component: '',
  icon: '',
  permissionCode: '',
  parentId: 0,
  sort: 0,
  type: 1,
  visible: true
})

export default {
  name: 'SystemMenus',
  data() {
    return {
      loading: false,
      saving: false,
      dialogVisible: false,
      tree: [],
      flat: [],
      form: emptyForm(),
      rules: {
        name: [{ required: true, message: '请输入菜单名称', trigger: 'blur' }],
        path: [{ required: true, message: '请输入路由路径', trigger: 'blur' }]
      }
    }
  },
  computed: {
    parentOptions() {
      return this.flat.filter(item => !this.form.id || item.id !== this.form.id)
    }
  },
  created() {
    this.loadData()
  },
  methods: {
    flatten(items, result = []) {
      items.forEach(item => {
        result.push(item)
        if (item.children && item.children.length) {
          this.flatten(item.children, result)
        }
      })
      return result
    },
    async loadData() {
      this.loading = true
      try {
        this.tree = await menuApi.tree()
        this.flat = this.flatten(this.tree)
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
      this.form = {
        id: row.id,
        name: row.name,
        path: row.path,
        component: row.component,
        icon: row.icon,
        permissionCode: row.permissionCode,
        parentId: row.parentId,
        sort: row.sort,
        type: row.type,
        visible: row.visible
      }
      this.dialogVisible = true
      this.$nextTick(() => this.$refs.form && this.$refs.form.clearValidate())
    },
    submit() {
      this.$refs.form.validate(async valid => {
        if (!valid) return
        this.saving = true
        try {
          if (this.form.id) {
            await menuApi.update(this.form.id, this.form)
          } else {
            await menuApi.create(this.form)
          }
          this.$message.success('保存成功')
          this.dialogVisible = false
          await this.loadData()
        } finally {
          this.saving = false
        }
      })
    },
    handleDelete(row) {
      this.$confirm(`确认删除菜单「${row.name}」？`, '提示', { type: 'warning' })
        .then(async () => {
          await menuApi.remove(row.id)
          this.$message.success('删除成功')
          await this.loadData()
        })
        .catch(() => {})
    }
  }
}
</script>
