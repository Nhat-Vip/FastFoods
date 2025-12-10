<script setup>
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'

const route = useRoute()
const router = useRouter()

// Xác định menu đang active và tiêu đề trang
const activeMenu = computed(() => {
  const path = route.path
  if (path.includes('users')) return 'users'
  if (path.includes('orders')) return 'orders'
  if (path.includes('combos')) return 'combos'
  if (path.includes('fastfood')) return 'fastfood'
  if(path.includes('categories')) return 'categories'
  return ''
})

const pageTitle = computed(() => {
  switch (activeMenu.value) {
    case 'users': return 'Quản lý Người dùng'
    case 'orders': return 'Quản lý Đơn hàng'
    case 'combos': return 'Quản lý Combo'
    case 'fastfood': return 'Quản lý Fastfood'
    case 'categories': return 'Quản lý Danh mục'
    default: return 'Admin Dashboard'
  }
})

const logout = () => {
  // Xử lý đăng xuất sau này
  alert('Đã đăng xuất!')
  router.push('/login')
}
</script>
<template>
<div class="admin-layout">
    <!-- Sidebar bên trái -->
    <aside class="sidebar">
      <div class="logo">
        <h2>
            <router-link to="/" class="fs-4 text-decoration-none mx-auto text-white fw-bold">Dashboard</router-link>
        </h2>
      </div>
      <nav class="menu">
        <router-link to="/dashboard/users" class="menu-item" :class="{ active: activeMenu === 'users' }">
          Quản lý User
        </router-link>
        <router-link to="/dashboard/orders" class="menu-item" :class="{ active: activeMenu === 'orders' }">
          Quản lý Order
        </router-link>
        <router-link to="/dashboard/combos" class="menu-item" :class="{ active: activeMenu === 'combos' }">
          Quản lý Combo
        </router-link>
        <router-link to="/dashboard/fastfood" class="menu-item" :class="{ active: activeMenu === 'fastfood' }">
          Quản lý Fastfood
        </router-link>
        <router-link to="/dashboard/categories" class="menu-item" :class="{ active: activeMenu === 'categories' }">
          Quản lý Danh mục
        </router-link>
      </nav>
    </aside>

    <!-- Nội dung bên phải -->
    <main class="main-content">
      <header class="topbar">
        <h1>{{ pageTitle }}</h1>
        <!-- <div class="user-info">
          Xin chào, <strong>Admin</strong>
          <button @click="logout" class="btn-logout">Đăng xuất</button>
        </div> -->
      </header>

      <div class="content">
        <!-- Nội dung của từng trang sẽ được render ở đây -->
        <router-view v-slot="{ Component }">
          <transition name="fade" mode="out-in">
            <component :is="Component" v-if="Component" />
            <div v-else class="empty-state">
              <i class="fas fa-mouse-pointer fa-3x"></i>
              <h3>Chọn một mục từ menu bên trái</h3>
              <p>Để xem và quản lý nội dung</p>
            </div>
          </transition>
        </router-view>
      </div>
    </main>
  </div>
</template>
<style scoped>
.admin-layout {
  display: flex;
  min-height: 100vh;
  background: #f4f6f9;
}

.sidebar {
  width: 250px;
  background: #2c3e50;
  color: white;
  padding: 20px 0;
  min-height: 100vh;
}

.logo h2 {
  text-align: center;
  margin: 0 0 30px 0;
  padding-bottom: 15px;
  border-bottom: 1px solid #34495e;
  font-size: 22px;
}

.menu-item {
  display: block;
  padding: 14px 24px;
  color: #ecf0f1;
  text-decoration: none;
  transition: all 0.3s;
}

.menu-item:hover {
  background: #34495e;
}

.menu-item.active {
  background: #2874f0;
  font-weight: bold;
}

.main-content {
  flex: 1;
  display: flex;
  flex-direction: column;
}

.topbar {
  background: white;
  padding: 15px 30px;
  box-shadow: 0 2px 10px rgba(0,0,0,0.1);
  display: flex;
  justify-content: space-between;
  align-items: center;
  z-index: 10;
}

.topbar h1 {
  margin: 0;
  color: #2c3e50;
  font-size: 22px;
}

.user-info {
  font-size: 15px;
}

.btn-logout {
  margin-left: 15px;
  background: #e74c3c;
  color: white;
  border: none;
  padding: 6px 12px;
  border-radius: 4px;
  cursor: pointer;
}

.content {
  height: 100vh;
  padding: 30px;
  flex: 1;
  background: #f4f6f9;
}

/* Responsive */
@media (max-width: 992px) {
  .sidebar {
    width: 80px;
    padding: 20px 0;
  }
  .logo h2, .menu-item span {
    display: none;
  }
  .menu-item {
    text-align: center;
  }
  .main-content {
    margin-left: 80px;
  }
}
</style>