<template>
  <div class="page-container">
    <div class="page-card">
      <div class="table-toolbar">
        <span class="text-muted">插件可在不修改主程序的情况下扩展库存预警、第三方集成等能力</span>
        <el-button icon="el-icon-refresh" @click="loadData">刷新</el-button>
      </div>

      <el-table v-loading="loading" :data="list" border stripe>
        <el-table-column prop="name" label="插件名称" min-width="150" />
        <el-table-column prop="id" label="插件标识" min-width="150" />
        <el-table-column prop="category" label="分类" width="130" />
        <el-table-column prop="description" label="说明" min-width="240" show-overflow-tooltip />
        <el-table-column prop="version" label="版本" width="90" align="center" />
        <el-table-column prop="author" label="作者" width="120" />
        <el-table-column prop="builtIn" label="来源" width="90" align="center">
          <template slot-scope="{ row }">
            <el-tag :type="row.builtIn ? 'info' : 'warning'" size="mini">{{ row.builtIn ? '内置' : '外部' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="enabled" label="启用" width="90" align="center">
          <template slot-scope="{ row }">
            <el-switch
              :value="row.enabled"
              :disabled="!$store.getters.permissions.includes('*') && !$store.getters.permissions.includes('system:plugin:edit')"
              @change="value => handleToggle(row, value)"
            />
          </template>
        </el-table-column>
        <el-table-column label="操作" width="180" fixed="right">
          <template slot-scope="{ row }">
            <el-button v-permission="'system:plugin:edit'" type="text" @click="openConfig(row)">配置</el-button>
            <el-button v-permission="'system:plugin:edit'" type="text" @click="handleExecute(row)">执行</el-button>
          </template>
        </el-table-column>
      </el-table>
    </div>

    <el-dialog :title="`插件配置 - ${current.name || ''}`" :visible.sync="dialogVisible" width="560px">
      <el-form label-width="120px">
        <el-form-item v-for="(item, index) in settingRows" :key="index" :label="item.key || '新增配置'">
          <div class="setting-row">
            <el-input v-if="!item.key" v-model="item.key" placeholder="配置项名称" style="width: 42%" />
            <span v-else class="setting-key">{{ item.key }}</span>
            <el-input v-model="item.value" placeholder="配置值" style="flex: 1" />
            <el-button type="text" class="text-danger" @click="settingRows.splice(index, 1)">删除</el-button>
          </div>
        </el-form-item>
        <el-form-item>
          <el-button type="text" icon="el-icon-plus" @click="settingRows.push({ key: '', value: '' })">新增配置项</el-button>
        </el-form-item>
      </el-form>
      <span slot="footer">
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="saveSettings">保存</el-button>
      </span>
    </el-dialog>

    <el-dialog title="插件执行结果" :visible.sync="resultVisible" width="520px">
      <el-descriptions v-if="result" :column="1" border size="small">
        <el-descriptions-item label="执行结果">
          <el-tag :type="result.success ? 'success' : 'danger'" size="mini">{{ result.success ? '成功' : '失败' }}</el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="说明">{{ result.message }}</el-descriptions-item>
        <el-descriptions-item label="耗时">{{ result.durationMs }} ms</el-descriptions-item>
        <el-descriptions-item label="执行时间">{{ formatDateTime(result.executedAt) }}</el-descriptions-item>
      </el-descriptions>
    </el-dialog>
  </div>
</template>

<script>
import { pluginApi } from '@/api/system'

export default {
  name: 'SystemPlugins',
  data() {
    return {
      loading: false,
      saving: false,
      dialogVisible: false,
      resultVisible: false,
      list: [],
      current: {},
      settingRows: [],
      result: null
    }
  },
  created() {
    this.loadData()
  },
  methods: {
    formatDateTime(value) {
      return value ? value.replace('T', ' ').substring(0, 19) : '—'
    },
    async loadData() {
      this.loading = true
      try {
        this.list = await pluginApi.list()
      } finally {
        this.loading = false
      }
    },
    async handleToggle(row, enabled) {
      try {
        await pluginApi.update(row.id, { enabled, settings: row.settings || {} })
        this.$message.success(enabled ? '插件已启用' : '插件已停用')
        this.loadData()
      } catch (error) {
        this.loadData()
      }
    },
    openConfig(row) {
      this.current = row
      this.settingRows = Object.keys(row.settings || {}).map(key => ({ key, value: row.settings[key] }))
      this.dialogVisible = true
    },
    async saveSettings() {
      const settings = {}
      this.settingRows.forEach(item => {
        if (item.key) settings[item.key] = item.value
      })
      this.saving = true
      try {
        await pluginApi.update(this.current.id, { enabled: this.current.enabled, settings })
        this.$message.success('插件配置已保存')
        this.dialogVisible = false
        this.loadData()
      } finally {
        this.saving = false
      }
    },
    async handleExecute(row) {
      this.result = await pluginApi.execute(row.id)
      this.resultVisible = true
    }
  }
}
</script>

<style scoped>
.setting-row {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
}
.setting-key {
  width: 42%;
  color: #606266;
}
</style>
