import axios from "axios";
import { useAuthStore } from "./stores/auth";
import { AlertError } from "./Notification";
import { useRoute } from "vue-router";


const api = axios.create({
    baseURL: "https://fastfoods-isb7.onrender.com/api"
});

api.interceptors.request.use(config => {
    const token = localStorage.getItem("token");

    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }   

    return config;
});
api.interceptors.response.use(
    response => response,
    error => {
        if (error.response?.status === 401) {
            const auth = useAuthStore();
            auth.logout();
            window.location.href = '/login'
        }
        if (error.response?.status === 429) {
            AlertError("Bạn đã gửi quá nhiều yêu cầu. Vui lòng thử lại sau.");
        }
        return Promise.reject(error)
    }
)

export default api;
