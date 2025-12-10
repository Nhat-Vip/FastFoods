import { defineStore } from "pinia";
import { ref } from "vue";

export const useAuthStore = defineStore("auth", () => {
    const isLogin = ref(localStorage.getItem("login") === "true");
    const userName = ref(localStorage.getItem("userName") ?? "");
    const userId = ref(localStorage.getItem("userId") ?? "");
    const userRole = ref(localStorage.getItem("role") ?? "Customer")

    function login(name,id,role,expiresIn = 3600) {
        localStorage.setItem("login", "true");
        localStorage.setItem("userName", name);
        localStorage.setItem("userId", id);
        localStorage.setItem("role", role);
        localStorage.setItem("expiresIn", Date.now() + expiresIn * 1000);


        isLogin.value = true;
        userName.value = name;
        userId.value = id;
        userRole.value = role;

        setTimeout(() => {
            logout()
        }, expiresIn * 1000);
    }
    function checkAuth() {
        const authExpires = localStorage.getItem("expiresIn");
        if (Date.now() > authExpires || !authExpires) {
            logout();
        }
    }
    function logout() {
        localStorage.clear();
        isLogin.value = false;
        userName.value = "";
        userId.value = "";
        userRole.value = "Customer";
    }
    checkAuth();
    return { isLogin, userName, userId,userRole, login, logout };
});
