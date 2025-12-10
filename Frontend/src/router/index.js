import CategoryManager from "@/components/Admin/CategoryManager.vue";
import ComboManager from "@/components/Admin/ComboManager.vue";
import FoodManager from "@/components/Admin/FoodManager.vue";
import OrderManager from "@/components/Admin/OrderManager.vue";
import UserManager from "@/components/Admin/UserManager.vue";
import Cart from "@/components/Cart.vue";
import Checkout from "@/components/Checkout.vue";
import FoodDetail from "@/components/FoodDetail.vue";
import Home from "@/components/Home.vue";
import Login from "@/components/Login.vue";
import Order from "@/components/Order.vue";
import Profile from "@/components/Profile.vue";
import AdminLayout from "@/layouts/AdminLayout.vue";
import { createRouter, createWebHistory } from "vue-router";

const routes = [
    {
        path: "/",
        redirect: "/home"
    },
    {
        path: "/home",
        name: "Home",
        component: Home,
    },
    {
        path: "/login",
        name: "Login",
        component: Login,
    },
    {
        path: "/order",
        name: "Order",
        component: Order,
        meta:{requiresAuth: true}
    },
    {
        path: "/profile",
        name: "Profile",
        component: Profile,
        meta:{requiresAuth: true}
    },
    {
        path: "/food-detail",
        name: "FoodDetail",
        component: FoodDetail,
    },
    {
        path: "/checkout",
        name: "Checkout",
        component: Checkout,
        meta:{requiresAuth: true}
    },
    {
        path: "/dashboard",
        name: "Dashboard",
        component: AdminLayout,
        children: [
            { path: '', component: null, meta: { empty: true } },
            {
                path: "combos",
                name: "ComboManager",
                component: ComboManager
            },
            {
                path: "fastfood",
                name: "FoodManager",
                component: FoodManager
            },
            {
                path: "orders",
                name: "OrderManager",
                component: OrderManager
            },
            {
                path: "users",
                name: "UserManager",
                component: UserManager
            },
            {
                path: "categories",
                name: "CategoryManager",
                component: CategoryManager
            }
        ],
        meta: { requiresAuth: true, role: "Admin" }
    },
    
]

const router = createRouter({
    history: createWebHistory(),
    routes
});

router.beforeEach((to, from, next) => {
    const token = localStorage.getItem('token');
    const userRole = localStorage.getItem('role');  // role được lưu sau khi login

    if (to.meta.requiresAuth && !token) {
        return next('/login');
    }

    if (to.meta.role && to.meta.role !== userRole) {
        return next('/403'); // hoặc redirect về trang lỗi
    }

    next();
});
export default router
