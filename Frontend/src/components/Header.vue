<script setup>
import router from '@/router';
import { computed ,ref} from 'vue';
import { RouterLink } from 'vue-router';
import { useAuthStore } from '@/stores/auth';
import Cart from './Cart.vue';

const auth = useAuthStore();

const handleLogout = () => {
    auth.logout()

    router.push("/");
}
</script>
<template>
    <header>
        <nav class="navbar navbar-expand-sm header">
            <router-link to="/" class="logo">FastFood</router-link>
            <ul class="navbar-nav w-100 d-flex justify-content-between px-4">
                <div class="d-flex gap-2">
                    <li class="nav-item">
                        <router-link class="nav-link" to="/order">Đơn hàng</router-link>
                    </li>
                    <li class="nav-item" v-if="auth.userRole == 'Admin'">
                        <router-link class="nav-link" to="/dashboard">Admin</router-link>
                    </li>
                </div>
                <div class="d-flex gap-2" v-if="!auth.isLogin">
                    <li class="nav-item">
                        <router-link to="/login" class="nav-link btn btn-info">Đăng nhập</router-link>
                    </li>
                    <li class="nav-item">
                        <router-link to="/login" class="nav-link btn btn-success">Đăng ký</router-link>
                    </li>
                </div>
                <div class="profile" v-if="auth.isLogin">
                    <li class="nav-item dropdown">
                        <a href="#" class="nav-link dropdown-toggle" id="userDropdown" data-bs-toggle="dropdown">
                            <span class="fs-5 fw-bold text-white">{{ auth.userName }}</span>
                        </a>
                        <ul class="dropdown-menu dropdown-menu-end shadow">
                            <li><router-link to="/profile" class="dropdown-item">Thông tin cá nhân</router-link></li>
                            <li><hr class="dropdown-divider" /></li>
                             <li>
                                <button @click="handleLogout()" class="dropdown-item text-danger">
                                    Đăng xuất
                                </button>
                            </li>
                        </ul>
                    </li>
                </div>
            </ul>
        </nav>
    </header>
    <cart></cart>
</template>
<style scoped>
.header{
    background-color: rgba(225, 77, 23, 0.889);
    padding: 10px 20px;
}
.logo{
    text-decoration: none;
    font-size: 28px;
    font-weight: bold;
    color: white;
}
a:hover{
    background-color: transparent;
}
.nav-link{
    position: relative;
    padding: 5px 10px;
    font-size: 17px;
    font-weight: 800;
    color: white;
    border-radius: 10px;
    z-index: 100;
}
.nav-link:focus{
    color: white;
}
.nav-link:hover{
    color: rgba(241, 219, 212, 0.889);
}
.dropdown-item:hover{
    opacity: .8;
}
</style>