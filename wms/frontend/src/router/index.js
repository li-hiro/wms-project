import Vue from 'vue'
import VueRouter from 'vue-router'
import Layout from '@/layout/index.vue'
import store from '@/store'

Vue.use(VueRouter)

/**
 * 静态路由表，meta.permission 与后端权限编码一一对应。
 * 导航守卫会校验登录态与权限，未授权页面跳转到 403。
 */
export const routes = [
  {
    path: '/',
    component: Layout,
    redirect: '/dashboard',
    children: [
      {
        path: 'dashboard',
        name: 'Dashboard',
        component: () => import('@/views/dashboard/index.vue'),
        meta: { title: '工作台', icon: 'el-icon-odometer', permission: 'dashboard:view' }
      },
      {
        path: 'master/materials',
        name: 'Materials',
        component: () => import('@/views/master/Materials.vue'),
        meta: { title: '物料管理', permission: 'material:view' }
      },
      {
        path: 'master/categories',
        name: 'Categories',
        component: () => import('@/views/master/Categories.vue'),
        meta: { title: '物料分类', permission: 'category:view' }
      },
      {
        path: 'master/suppliers',
        name: 'Suppliers',
        component: () => import('@/views/master/Suppliers.vue'),
        meta: { title: '供应商管理', permission: 'supplier:view' }
      },
      {
        path: 'master/customers',
        name: 'Customers',
        component: () => import('@/views/master/Customers.vue'),
        meta: { title: '客户管理', permission: 'customer:view' }
      },
      {
        path: 'warehouse/warehouses',
        name: 'Warehouses',
        component: () => import('@/views/warehouse/Warehouses.vue'),
        meta: { title: '仓库管理', permission: 'warehouse:view' }
      },
      {
        path: 'warehouse/locations',
        name: 'Locations',
        component: () => import('@/views/warehouse/Locations.vue'),
        meta: { title: '库位管理', permission: 'location:view' }
      },
      {
        path: 'inbound',
        name: 'Inbound',
        component: () => import('@/views/inbound/index.vue'),
        meta: { title: '入库作业', permission: 'inbound:view' }
      },
      {
        path: 'outbound',
        name: 'Outbound',
        component: () => import('@/views/outbound/index.vue'),
        meta: { title: '出库作业', permission: 'outbound:view' }
      },
      {
        path: 'inventory',
        name: 'Inventory',
        component: () => import('@/views/inventory/Stock.vue'),
        meta: { title: '库存查询', permission: 'inventory:view' }
      },
      {
        path: 'inventory/transactions',
        name: 'Transactions',
        component: () => import('@/views/inventory/Transactions.vue'),
        meta: { title: '库存流水', permission: 'inventory:view' }
      },
      {
        path: 'inventory/alerts',
        name: 'Alerts',
        component: () => import('@/views/inventory/Alerts.vue'),
        meta: { title: '库存预警', permission: 'inventory:view' }
      },
      {
        path: 'stocktake',
        name: 'StockTake',
        component: () => import('@/views/stocktake/index.vue'),
        meta: { title: '库存盘点', permission: 'stocktake:view' }
      },
      {
        path: 'transfer',
        name: 'Transfer',
        component: () => import('@/views/transfer/index.vue'),
        meta: { title: '库存调拨', permission: 'transfer:view' }
      },
      {
        path: 'reports/stock',
        name: 'StockSummary',
        component: () => import('@/views/reports/StockSummary.vue'),
        meta: { title: '库存汇总报表', permission: 'report:view' }
      },
      {
        path: 'reports/inout',
        name: 'InOutSummary',
        component: () => import('@/views/reports/InOutSummary.vue'),
        meta: { title: '收发存报表', permission: 'report:view' }
      },
      {
        path: 'system/users',
        name: 'Users',
        component: () => import('@/views/system/Users.vue'),
        meta: { title: '用户管理', permission: 'system:user:view' }
      },
      {
        path: 'system/roles',
        name: 'Roles',
        component: () => import('@/views/system/Roles.vue'),
        meta: { title: '角色权限', permission: 'system:role:view' }
      },
      {
        path: 'system/menus',
        name: 'Menus',
        component: () => import('@/views/system/Menus.vue'),
        meta: { title: '菜单管理', permission: 'system:menu:view' }
      },
      {
        path: 'system/logs',
        name: 'Logs',
        component: () => import('@/views/system/Logs.vue'),
        meta: { title: '操作日志', permission: 'system:log:view' }
      },
      {
        path: 'system/plugins',
        name: 'Plugins',
        component: () => import('@/views/system/Plugins.vue'),
        meta: { title: '插件管理', permission: 'system:plugin:view' }
      }
    ]
  },
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/login/index.vue'),
    meta: { title: '登录' }
  },
  {
    path: '/403',
    name: 'Forbidden',
    component: () => import('@/views/error/403.vue'),
    meta: { title: '无访问权限' }
  },
  {
    path: '*',
    name: 'NotFound',
    component: () => import('@/views/error/404.vue'),
    meta: { title: '页面不存在' }
  }
]

const router = new VueRouter({
  mode: 'hash',
  routes,
  scrollBehavior: () => ({ y: 0 })
})

const hasPermission = permission => {
  if (!permission) return true
  const permissions = store.getters.permissions || []
  return permissions.includes('*') || permissions.includes(permission)
}

router.beforeEach(async (to, from, next) => {
  const token = store.getters.token

  if (to.path === '/login') {
    next(token ? '/dashboard' : undefined)
    return
  }

  if (!token) {
    next(`/login?redirect=${encodeURIComponent(to.fullPath)}`)
    return
  }

  // 首次进入时同步用户资料与菜单
  if (!store.getters.profile || !store.getters.profile.userName) {
    try {
      await store.dispatch('user/loadProfile')
      await store.dispatch('user/loadMenus')
    } catch (error) {
      await store.dispatch('user/resetToken')
      next(`/login?redirect=${encodeURIComponent(to.fullPath)}`)
      return
    }
  }

  if (to.path === '/403' || hasPermission(to.meta && to.meta.permission)) {
    next()
    return
  }

  next('/403')
})

export default router
