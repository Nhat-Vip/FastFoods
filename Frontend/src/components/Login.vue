<script setup>
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import api from '@/api'
import { AlertSuccess,AlertError } from '@/Notification'
import { email, required ,helpers} from '@vuelidate/validators'
import useVuelidate from '@vuelidate/core'
import { useAuthStore } from '@/stores/auth'


const auth = useAuthStore();
const router = useRouter()
const isLogin = ref(true) // true = đang ở tab đăng nhập
const errorLogin = ref("");
const errorMessages = ref([]);
// Form đăng nhập
const login = reactive({
  email: '',
  password: ''
})

// Form đăng ký (theo đúng model User của bạn)
const register = reactive({
  fullName: '',
  username: '',
  email: '',
  phone: '',
  address: '',
  password: '',
  confirmPassword: ''
})
const phoneValidator = helpers.withMessage(
  'Số điện thoại không hợp lệ',
  value => /^0\d{9}$/.test(value)
);
const rules = {
  register: {
    fullName: { required },
    username: { required },
    email: { email, required },
    phone: { required ,phoneValidator},
    address: { required },
    password: { required },
    confirmPassword: {required}
  }
}

const v$ = useVuelidate(rules, { register });


// Xử lý đăng nhập 
const handleLogin = async() => {
  // Gọi API đăng nhập ở đây
  try {
    const res = await api.post("/login", login);
    console.log("Login: ", res.data);
    localStorage.setItem("token", res.data.token);
    localStorage.setItem("role",res.data.role);
    localStorage.setItem("userName", res.data.userName);
    localStorage.setItem("login", "true");
    auth.login(res.data.userName, res.data.userId, res.data.role);
    AlertSuccess("Đăng nhập thành công");
    router.push("/");
  }
  catch (e) {
    if (e.response && e.response.status === 400) {
      errorLogin.value = e.response.data.message;
    }
    else {
      errorLogin.value = "Có lỗi xảy ra khi đăng nhập vui lòng thử lại sau"
    }
  }
}

// Xử lý đăng ký
const handleRegister = async () => {
  try {
    if (register.password !== register.confirmPassword) return
    const isValid = await v$.value.register.$validate();
    if (!isValid) return;

    const userData = {
      Username: register.username,
      Email: register.email,
      PasswordHash: register.password,
      FullName: register.fullName,
      Phone: register.phone,
      Address: register.address
    }

    console.log('Dữ liệu gửi đi:', userData);
    const res = await api.post("/register", userData);
    AlertSuccess("Đăng ký thành công! Vui lòng đăng nhập.");
    errorMessages.value = "" // reset lỗi
    isLogin.value = true // chuyển về tab đăng nhập
    // Reset form
    Object.keys(register).forEach(key => register[key] = '')
  }
  catch (e) {
    if (e.response && e.response.status === 400) {
      e.response.data.errors.forEach(err => errorMessages.value.push(err));
    }
    else {
      errorMessages.value.push("Có lỗi xảy ra vui lòng thử lại sau");
    }
  }
}
</script>
<template>
<div class="auth-container">
    <div class="auth-box">
      <h2>Chào mừng đến với FastFood</h2>

      <!-- Tabs -->
      <div class="tabs">
        <button 
          :class="{ active: isLogin }" 
          @click="isLogin = true">
          Đăng nhập
        </button>
        <button 
          :class="{ active: !isLogin }" 
          @click="isLogin = false">
          Đăng ký
        </button>
      </div>

      <!-- Form Đăng nhập -->
      <form v-if="isLogin" @submit.prevent="handleLogin" class="auth-form">
        <div v-if="errorLogin">
          <span class="text-danger">{{ errorLogin }}</span>
        </div>
        <input type="text" v-model="login.email" placeholder="Tên đăng nhập hoặc Email" required />
        <input type="password" v-model="login.password" placeholder="Mật khẩu" required />
        <button type="submit" class="btn-primary">Đăng nhập</button>
      </form>

      <!-- Form Đăng ký -->
      <form v-else @submit.prevent="handleRegister" class="auth-form">
        <input type="text" v-model="register.fullName" placeholder="Họ và tên *" @blur="v$.register.fullName.$touch" />
        <div v-if="v$.register.fullName.$error" class="error">
          {{ v$.register.fullName.$errors[0].$message }}
        </div>
        <input type="text" v-model="register.username" placeholder="Tên đăng nhập *" @blur="v$.register.username.$touch"/>
        <div v-if="v$.register.username.$error" class="error">
          {{ v$.register.username.$errors[0].$message }}
        </div>
        <input type="email" v-model="register.email" placeholder="Email *" @blur="v$.register.email.$touch"/>
        <div v-if="v$.register.email.$error" class="error">
          {{ v$.register.email.$errors[0].$message }}
        </div>
        <input type="tel" v-model="register.phone" placeholder="Số điện thoại *" @blur="v$.register.phone.$touch"/>
        <div v-if="v$.register.phone.$error" class="error">
          {{ v$.register.phone.$errors[0].$message }}
        </div>
        <input type="text" v-model="register.address" placeholder="Địa chỉ nhận hàng *" @blur="v$.register.address.$touch"/>
        <div v-if="v$.register.address.$error" class="error">
          {{ v$.register.address.$errors[0].$message }}
        </div>
        <input type="password" v-model="register.password" placeholder="Mật khẩu *" @blur="v$.register.password.$touch"/>
        <div v-if="v$.register.password.$error" class="error">
          {{ v$.register.password.$errors[0].$message }}
        </div>
        <input type="password" v-model="register.confirmPassword" placeholder="Nhập lại mật khẩu *" @blur="v$.register.confirmPassword.$touch"/>
        <div v-if="v$.register.confirmPassword.$error" class="error">
          {{ v$.register.confirmPassword.$errors[0].$message }}
        </div>

        <button type="submit" class="btn-primary" :disabled="register.password !== register.confirmPassword">
          Đăng ký ngay
        </button>

        <p v-if="register.password && register.password !== register.confirmPassword" class="error">
          Mật khẩu không khớp!
        </p>
        <div v-if="errorMessages.length > 0" class="error">
            <p v-for="err in errorMessages" :key="err">{{ err }}</p>
        </div>
      </form>
    </div>
  </div>
</template>
<style scoped>
.auth-container {
  min-height: 100vh;
  display: flex;
  justify-content: center;
  background: #f5f5f5;
  padding: 20px;
}

.auth-box {
  background: white;
  height: max-content;
  padding: 30px 40px;
  border-radius: 10px;
  box-shadow: 0 4px 20px rgba(0,0,0,0.1);
  width: 100%;
  max-width: 620px;
}

h2 {
  text-align: center;
  margin-bottom: 20px;
  color: #333;
}

.tabs {
  display: flex;
  margin-bottom: 25px;
  border-bottom: 2px solid #eee;
}

.tabs button {
  flex: 1;
  padding: 12px;
  border: none;
  background: none;
  font-size: 16px;
  cursor: pointer;
  color: #666;
}

.tabs button.active {
  color: #2874f0;
  border-bottom: 3px solid #2874f0;
  font-weight: bold;
}

.auth-form input {
  width: 100%;
  padding: 12px 14px;
  margin: 10px 0;
  border: 1px solid #ddd;
  border-radius: 6px;
  font-size: 15px;
}

.auth-form input:focus {
  outline: none;
  border-color: #2874f0;
}

.btn-primary {
  width: 100%;
  padding: 13px;
  margin-top: 15px;
  background: #2874f0;
  color: white;
  border: none;
  border-radius: 6px;
  font-size: 16px;
  cursor: pointer;
}

.btn-primary:hover {
  background: #1160d1;
}

.btn-primary:disabled {
  background: #ccc;
  cursor: not-allowed;
}

.error {
  color: red;
  font-size: 14px;
  margin-top: 8px;
}

.note {
  text-align: center;
  color: #888;
  font-size: 13px;
  margin-top: 20px;
}
</style>