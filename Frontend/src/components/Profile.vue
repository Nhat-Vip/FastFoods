<script setup>
import { ref, reactive, onMounted, computed } from 'vue'
import api from '@/api';
import { useAuthStore } from '@/stores/auth';
import { required ,helpers, email} from '@vuelidate/validators';
import useVuelidate from '@vuelidate/core';


var auth = useAuthStore();
// Giả lập dữ liệu user từ localStorage (sau này lấy từ API)
const user = reactive({
    UserId: 1,
    Username: '',
    Email: '',
    FullName: '',
    Phone: '',
    Address: '',
    DateOfBirth: '',
    UserRole: 1
});

// const form = reactive({
//     UserId: null,
//     FullName: '',
//     Phone: '',
//     Address: '',
//     Username:'',
//     DateOfBirth: null
// })

const phoneValidator = helpers.withMessage(
    'Số điện thoại không hợp lệ',
    value => /^0\d{9}$/.test(value)
);
const rules = {
    user: {
        FullName: { required },
        Phone: { required, phoneValidator },
        Address: { required },
        Username: { required },
        Email:{required, email}
    }
}

const v$ = useVuelidate(rules, { user });

const isSaving = ref(false)
const successMessage = ref('')
const errorMessage = ref('')

const isAdmin = computed(() => user.UserRole === 2)

// Load dữ liệu vào form khi mở trang
// const loadUserData = () => {
//     console.log("user: ", user)
//     form.FullName = user.FullName
//     form.Phone = user.Phone
//     form.Address = user.Address
//     form.Username = user.Username
//     form.DateOfBirth = user.DateOfBirth || null
//     successMessage.value = ''
//     errorMessage.value = ''
// }

const fetchUser = async () => {
    try {
        const res = await api.get(`/users/${Number(auth.userId)}`);
        console.log("user Data: ", res.data);
        user.UserId = res.data.userId;
        user.FullName = res.data.fullName;
        user.Username = res.data.username;
        user.Email = res.data.email;
        user.Phone = res.data.phone;
        user.Address = res.data.address;
        user.DateOfBirth = res.data.dateOfBirth.split("T")[0];
        user.UserRole = res.data.userRole;
    }
    catch (e) {
        console.log("Có lỗi xảy ra khi tải dữ liệu người dùng", e);
    }
}

const updateProfile = async () => {
    isSaving.value = true
    successMessage.value = ''
    errorMessage.value = ''

    try {
        // Gọi API cập nhật ở đây (ví dụ: PUT /api/user/profile)
        // await api.put('/user/profile', form)
        const isValid = v$.value.user.$validate();
        if (!isValid) return;
        // Giả lập thành công
        const res = await api.put(`/users/${Number(auth.userId)}`, user)
        console.log("Res Data: ",res.data);
        // Cập nhật lại user
        successMessage.value = 'Cập nhật thông tin thành công!'
        fetchUser();
    } catch (err) {
        console.log("Có lỗi xảy ra: ", err);
        errorMessage.value = 'Có lỗi xảy ra, vui lòng thử lại!'
    } finally {
        isSaving.value = false
    }
}

onMounted( async() => {
    fetchUser();
    // loadUserData();
})
</script>
<template>
    <div class="profile-container">
        <div class="profile-card">
            <h2>Thông tin cá nhân</h2>

            <!-- Avatar + Tên lớn -->
            <div class="profile-header">
                <div class="avatar">
                    <i class="fas fa-user-circle"></i>
                </div>
                <div class="mx-auto text-center">
                    <h3>{{ user.FullName }}</h3>
                    <p class="role" :class="{ admin: isAdmin }">
                        {{ isAdmin ? 'Quản trị viên' : 'Khách hàng' }}
                    </p>
                </div>
            </div>

            <!-- Form chỉnh sửa -->
            <form @submit.prevent="updateProfile" class="profile-form">
                <p v-if="successMessage" class="success">{{ successMessage }}</p>
                <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
                <div class="form-group">
                    <label>Họ và tên *</label>
                    <input v-model="user.FullName" type="text" @blur="v$.user.FullName.$touch" />
                    <div class="error" v-if="v$.user.FullName.$error">
                        <span>{{ v$.user.FullName.$errors[0].$message }}</span>
                    </div>
                </div>
                <div class="form-group">
                    <label>Tên đăng nhập</label>
                    <input v-model="user.Username" @blur="v$.user.Username.$touch" />
                    <div class="error" v-if="v$.user.Username.$error">
                        <span>{{ v$.user.Username.$errors[0].$message }}</span>
                    </div>
                </div>
                <div class="form-group">
                    <label>Số điện thoại *</label>
                    <input v-model="user.Phone" type="tel" maxlength="11" @blur="v$.user.Phone.$touch" />
                    <div class="error" v-if="v$.user.Phone.$error">
                        <span>{{ v$.user.Phone.$errors[0].$message }}</span>
                    </div>
                </div>

                <div class="form-group">
                    <label>Địa chỉ nhận hàng mặc định *</label>
                    <input v-model="user.Address" type="text" @blur="v$.user.Address.$touch" />
                    <div class="error" v-if="v$.user.Address.$error">
                        <span>{{ v$.user.Address.$errors[0].$message }}</span>
                    </div>
                </div>

                <div class="form-group">
                    <label>Ngày sinh</label>
                    <input v-model="user.DateOfBirth" type="date" />
                </div>

                <div class="form-row">
                    <div class="form-group half">
                        <label>Email</label>
                        <input v-model="user.Email" type="email" @blur="v$.user.Email.$touch" />
                        <div class="error" v-if="v$.user.Email.$error">
                            <span>{{ v$.user.Email.$errors[0].$message }}</span>
                        </div>
                    </div>
                </div>

                <div class="form-actions">
                    <button type="submit" class="btn-save" :disabled="isSaving">
                        {{ isSaving ? 'Đang lưu...' : 'Cập nhật thông tin' }}
                    </button>
                    <button type="button" @click="fetchUser" class="btn-cancel">
                        Hủy bỏ
                    </button>
                </div>

            </form>
        </div>
    </div>
</template>
<style scoped>
.profile-container {
    min-height: 100vh;
    background: #f5f5f5;
    padding: 30px 20px;
    display: flex;
    justify-content: center;
}

.profile-card {
    background: white;
    width: 100%;
    max-width: 600px;
    border-radius: 12px;
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.08);
    padding: 30px;
}

h2 {
    text-align: center;
    margin-bottom: 25px;
    color: #2c3e50;
}

.profile-header {
    display: flex;
    align-items: center;
    gap: 20px;
    margin-bottom: 30px;
    padding-bottom: 20px;
    border-bottom: 1px solid #eee;
}

.avatar i {
    font-size: 70px;
    color: #2874f0;
}

h3 {
    margin: 0;
    font-size: 24px;
}

.role {
    margin: 5px 0 0;
    color: #666;
    font-size: 16px;
    font-weight: 600;
}

.role.admin {
    color: #e74c3c;
    font-weight: bold;
}

.form-group {
    margin-bottom: 18px;
}

label {
    display: block;
    margin-bottom: 6px;
    font-weight: 600;
    color: #333;
}

input {
    width: 100%;
    padding: 12px 14px;
    border: 1px solid #ddd;
    border-radius: 6px;
    font-size: 15px;
}

input:focus {
    outline: none;
    border-color: #2874f0;
    box-shadow: 0 0 0 3px rgba(40, 116, 240, 0.15);
}

input:disabled {
    background: #f8f9fa;
    color: #666;
}

.form-row {
    display: flex;
    gap: 15px;
}

.half {
    flex: 1;
}

.form-actions {
    margin-top: 25px;
    display: flex;
    justify-content: end;
    gap: 12px;
}

.btn-save {
    background: #2874f0;
    color: white;
    padding: 12px 24px;
    border: none;
    border-radius: 6px;
    font-size: 16px;
    cursor: pointer;
}

.btn-save:disabled {
    background: #aaa;
    cursor: not-allowed;
}

.btn-cancel {
    background: rgba(218, 14, 14, 0.768);
    color: white;
    padding: 12px 24px;
    border: none;
    border-radius: 6px;
    cursor: pointer;
}

.success {
    color: #27ae60;
    margin-top: 15px;
    text-align: center;
}

.error {
    color: #e74c3c;
    margin-top: 15px;
    text-align: center;
}
</style>