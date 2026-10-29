<template>
  <div class="login-page">
    <div class="login-panel">
      <div class="login-brand">
        <h1>WMS 工厂仓储管理系统</h1>
        <p>仓库作业 · 库存管控 · 报表分析 · 权限管理</p>
      </div>
      <el-form ref="loginForm" :model="form" :rules="rules" class="login-form" @submit.native.prevent="submit">
        <el-form-item prop="userName">
          <el-input v-model="form.userName" prefix-icon="el-icon-user" placeholder="登录账号" />
        </el-form-item>
        <el-form-item prop="password">
          <el-input
            v-model="form.password"
            prefix-icon="el-icon-lock"
            type="password"
            show-password
            placeholder="登录密码"
            @keyup.enter.native="submit"
          />
        </el-form-item>
        <el-button type="primary" class="login-button" :loading="loading" @click="submit">登 录</el-button>
      </el-form>
      <div class="login-tip">
        演示账号：admin / admin123，keeper / keeper123，viewer / viewer123
      </div>
    </div>
  </div>
</template>

<script>
export default {
  name: 'Login',
  data() {
    return {
      loading: false,
      form: { userName: 'admin', password: 'admin123' },
      rules: {
        userName: [{ required: true, message: '请输入登录账号', trigger: 'blur' }],
        password: [{ required: true, message: '请输入登录密码', trigger: 'blur' }]
      }
    }
  },
  methods: {
    submit() {
      this.$refs.loginForm.validate(async valid => {
        if (!valid) return
        this.loading = true
        try {
          await this.$store.dispatch('user/login', this.form)
          const redirect = this.$route.query.redirect || '/dashboard'
          this.$router.replace(redirect)
        } catch (error) {
          // 错误提示已由请求拦截器统一处理
        } finally {
          this.loading = false
        }
      })
    }
  }
}
</script>

<style scoped>
.login-page {
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #1f2d3d 0%, #2c4557 60%, #3a6073 100%);
}

.login-panel {
  width: 400px;
  background: #fff;
  border-radius: 8px;
  padding: 40px 36px 28px;
  box-shadow: 0 12px 40px rgba(0, 0, 0, 0.2);
}

.login-brand {
  text-align: center;
  margin-bottom: 28px;
}

.login-brand h1 {
  margin: 0;
  font-size: 22px;
  color: #1f2d3d;
}

.login-brand p {
  margin: 8px 0 0;
  font-size: 12px;
  color: #909399;
}

.login-button {
  width: 100%;
  margin-top: 4px;
}

.login-tip {
  margin-top: 20px;
  font-size: 12px;
  color: #909399;
  text-align: center;
  line-height: 1.7;
}
</style>
