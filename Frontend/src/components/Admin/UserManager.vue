<script setup>
import api from '@/api'
import { AlertError, AlertSuccess } from '@/Notification'
import useVuelidate from '@vuelidate/core'
import { helpers, required } from '@vuelidate/validators'
import { computed, onMounted, onUpdated, ref } from 'vue'

const users = ref([])

// Modal
const isModalOpen = ref(false)
const isEditMode = ref(false)
const currentUser = ref(null)
const userToDelete = ref(null)
const showConfirm = ref(false)
const isDetailMode = ref(false)
const currentPage = ref(1);
const itemsPerPage = ref(6);
const totalPages = computed(() => {
    return Array.from({ length: Math.ceil(users.value.length / itemsPerPage.value) }, (_, i) => i + 1);
});
const maxPageIndex = ref(5);
const minPageIndex = computed(() => {
    const half = Math.floor(maxPageIndex.value / 2);
    let start = currentPage.value - half;

    if (start < 1) start = 1;

    if (start + maxPageIndex.value - 1 > totalPages.value.length) {
        start = Math.max(1, totalPages.value.length - maxPageIndex.value + 1);
    }

    return start;
});


const form = ref({
    userId: null,
    username: '',
    email: '',
    fullName: '',
    phone: '',
    address: '',
    dateOfBirth: null,
    userRole: 1,
    password: ''
})

const phoneValidator = helpers.withMessage(
    'Số điện thoại không hợp lệ',
    value => /^0\d{9}$/.test(value)
);

const rules = {
    form: {
        phone: {
            required: helpers.withMessage('Vui lòng nhập số điện thoại', required),
            phoneValidator
        }
    }
}

const v$ = useVuelidate(rules, { form });

// Mở modal chi tiết
const openDetailModal = (event,user) => {
    if(event.target.closest('.dropdown') ) return;

    isDetailMode.value = true;
    isEditMode.value = false;
    // currentUser.value = user
    form.value = {
        userId: user.userId,
        username: user.username,
        email: user.email,
        fullName: user.fullName,
        phone: user.phone,
        address: user.address,
        dateOfBirth: user.dateOfBirth?.split('T')[0],
        userRole: user.userRole,
        password: ''
    }
    isModalOpen.value = true
    console.log(form.value);
}

// Mở modal thêm
const openCreateModal = () => {
    isEditMode.value = false;
    isDetailMode.value = false;
    form.value = {userId: null, username: '', email: '', fullName: '', phone: '', address: '', dateOfBirth: null, userRole: 1, password: '' }
    isModalOpen.value = true
}

// Mở modal sửa
const openEditModal = (user) => {
    isDetailMode.value = false;
    isEditMode.value = true;
    currentUser.value = user
    form.value = {
        userId: user.userId,
        username: user.username,
        email: user.email,
        fullName: user.fullName,
        phone: user.phone,
        address: user.address,
        dateOfBirth: user.dateOfBirth?.split('T')[0],
        userRole: user.userRole,
        password: ''
    }
    isModalOpen.value = true;
    console.log(isDetailMode.value);
}


const closeModal = () => {
    isModalOpen.value = false
    currentUser.value = null
}

// Lưu (thêm/sửa)
const saveUser = async () => {
    try {
        const isValid = v$.value.form.$validate();
        if (!isValid) return;

        if (isEditMode.value) {
            Object.assign(currentUser.value, {
                username: form.value.username,
                fullName: form.value.fullName,
                email: form.value.email,
                phone: form.value.phone,
                address: form.value.address,
                dateOfBirth: form.value.dateOfBirth,
                userRole: form.value.userRole
            });
            await api.put(`/users/${currentUser.value.userId}`, form.value);

            AlertSuccess('Cập nhật thành công!');
        } else {
            const newUser = {
                userId: Math.max(...users.value.map(u => u.userId)) + 1,
                username: form.value.username,
                email: form.value.email,
                fullName: form.value.fullName,
                phone: form.value.phone,
                address: form.value.address,
                dateOfBirth: form.value.dateOfBirth,
                userRole: form.value.userRole,
                isActive: true,
                createdAt: new Date().toISOString(),
                passwordHash: form.value.password
            }
            const res = await api.post("/users", newUser);
            users.value.push(res.data);
            AlertSuccess("Thêm người dùng mới thành công!");
        }
        closeModal();
    }
    catch (e) {
        if (e.response && e.response.status === 400) {
            AlertError(e.response.data.message);
        }
        else {
            AlertError("Có lỗi xảy ra vui lòng thử lại");
            console.log("Có lỗi xảy ra", e);
        }
    }
}

// Xóa + Khóa/Mở
const confirmDelete = (user) => { userToDelete.value = user; showConfirm.value = true }
const deleteUser = async() => {
    try {

        await api.delete(`/users/${userToDelete.value.userId}`);

        users.value = users.value.filter(u => u.userId !== userToDelete.value.userId)
        AlertSuccess('Đã xóa người dùng!')
        showConfirm.value = false;
    }
    catch (e) {
        if (e.response && e.response.status === 400) {
            AlertError(e.response.data.message);
        }
        else {
            AlertError("Có lỗi xảy ra vui lòng thử lại");
        }
    }
}
const toggleActive = async (user) => {
    try {
        await api.put(`/users/lock/${user.userId}`);
        user.isActive = !user.isActive
        AlertSuccess(user.isActive ? 'Đã mở khóa' : 'Đã khóa tài khoản')
    }
    catch (e) {
        if (e.response && e.response.status === 400) {
            AlertError(e.response.data.message);
        }
        else {
            AlertError("Có lỗi xảy ra vui lòng thử lại");
        }
    }
}

// Helper
const formatDate = (date) => new Date(date).toLocaleDateString('vi-VN');


onMounted(async() => {
    const res = await api.get("/users");
    users.value = res.data;
})
</script>
<template>
    <div class="user-manager">
        <div class="header">
            <h2>Quản lý người dùng</h2>
            <button @click="openCreateModal" class="btn-add">
                Thêm người dùng
            </button>
        </div>

        <!-- Bảng danh sách -->
        <div class="table-container">
            <table class="user-table">
                <thead>
                    <tr>
                        <th class="text-center">#</th>
                        <!-- <th>Tên đăng nhập</th> -->
                        <th>Họ tên</th>
                        <th>Email</th>
                        <th>SĐT</th>
                        <th>Vai trò</th>
                        <th>Trạng thái</th>
                        <!-- <th>Ngày tạo</th> -->
                        <th class="text-center">Hành động</th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="user in users" :key="user.userId" @click="openDetailModal($event,user)">
                        <td class="text-center">{{ user.userId }}</td>
                        <!-- <td><strong>{{ user.username }}</strong></td> -->
                        <td>
                            <div class="user-info">
                                {{ user.fullName }}
                            </div>
                        </td>
                        <td>{{ user.email }}</td>
                        <td>{{ user.phone }}</td>
                        <td>
                            <span class="role-badge" :class="user.userRole === 2 ? 'admin' : 'customer'">
                                {{ user.userRole === 2 ? 'Quản trị viên' : 'Khách hàng' }}
                            </span>
                        </td>
                        <td class="status">
                            <span :class="user.isActive ? 'active' : 'inactive'"></span>
                            <span class="status-badge">
                                {{ user.isActive ? 'Hoạt động' : 'Bị khóa' }}
                            </span>
                        </td>
                        <!-- <td>{{ formatDate(user.createdAt) }}</td> -->
                        <td class="actions dropdown">
                            <button type="button" data-bs-toggle="dropdown"><i class="bi bi-three-dots-vertical"></i></button>
                            <ul class="dropdown-menu">
                                <li><button @click="openEditModal(user)" class="dropdown-item">Sửa</button></li>
                                <li><button @click="toggleActive(user)" class="dropdown-item">
                                    {{ user.isActive ? 'Khóa' : 'Mở' }}
                                </button></li>
                                <li><button @click="confirmDelete(user)" class="dropdown-item text-danger">Xóa</button></li>
                            </ul>
                        </td>
                    </tr>
                </tbody>
            </table>
            
        </div>
        <div class="navigation" v-if="totalPages">
            <button @click="currentPage = 1" :disabled="currentPage === 1" class="navigation-item previous">
                <i class="bi bi-chevron-double-left"></i>
            </button>
            <button @click="currentPage--" :disabled="currentPage === 1" class="navigation-item previous">
                <i class="bi bi-chevron-left"></i>
            </button>
            <button v-for="page in totalPages.slice(minPageIndex - 1, minPageIndex - 1 + maxPageIndex)" :key="page"
                @click="currentPage = page" class="navigation-item" :class="currentPage == page ? 'active' : ''">
                {{ page }}
            </button>
            <button @click="currentPage++" :disabled="currentPage === totalPages.length" class="navigation-item next">
                <i class="bi bi-chevron-right"></i>
            </button>
            <button @click="currentPage = totalPages.length" :disabled="currentPage === totalPages.length"
                class="navigation-item next">
                <i class="bi bi-chevron-double-right"></i>
            </button>
        </div>
        <!-- Modal Thêm / Sửa -->
        <Teleport to="body">
            <div v-if="isModalOpen" class="modal-overlay" @click="closeModal"></div>
            <div v-if="isModalOpen" class="user-modal ">
                <div class="modal-header">
                    <h3>{{ isEditMode ? 'Sửa người dùng' : isDetailMode ? 'Chi tiết thông tin người dùng' :'Thêm người dùng' }}</h3>
                    <button @click="closeModal" class="btn btn-close"></button>
                </div>

                <form @submit.prevent="saveUser" class="modal-form">
                    <div class="form-row">
                        <div class="form-group">
                            <label>Tên đăng nhập *</label>
                            <input v-model="form.username" placeholder="VD: VTV" required :disabled="Boolean(isDetailMode)"/>
                        </div>
                        <div class="form-group">
                            <label>Họ và tên *</label>
                            <input v-model="form.fullName" placeholder="VD: Nguyễn Văn A" required :disabled="isDetailMode"/>
                        </div>
                    </div>

                    <div class="form-row">
                        <div class="form-group">
                            <label>Email *</label>
                            <input v-model="form.email" type="email" placeholder="VD: example@gmail.com" required :disabled="isDetailMode"/>
                        </div>
                        <div class="form-group">
                            <label>Số điện thoại *</label>
                            <input v-model="form.phone" maxlength="11" @blur="v$.form.phone.$touch" placeholder="VD: 0xxxxxxxx" required :disabled="isDetailMode"/>
                            <div v-if="v$.form.phone.$error" class="text-danger">
                                {{ v$.form.phone.$errors[0].$message }}
                            </div>
                        </div>
                    </div>

                    <div class="form-group">
                        <label>Địa chỉ *</label>
                        <input v-model="form.address" placeholder="VD: Tổ 11, Khu phố A, Biên Hòa Đồng Nai" required :disabled="isDetailMode"/>
                    </div>

                    <div class="form-row">
                        <div class="form-group">
                            <label>Ngày sinh</label>
                            <input v-model="form.dateOfBirth" type="date" :disabled="isDetailMode"/>
                        </div>
                        <div class="form-group">
                            <label>Vai trò *</label>
                            <select v-model="form.userRole" :disabled="isDetailMode">
                                <option :value="1">Khách hàng</option>
                                <option :value="2">Quản trị viên</option>
                            </select>
                        </div>
                    </div>

                    <div class="form-group" v-if="!isEditMode && !isDetailMode">
                        <label>Mật khẩu *</label>
                        <input v-model="form.password" type="password" required :disabled="isDetailMode"/>
                    </div>

                    <div class="form-group" v-if="isEditMode && !isDetailMode">
                        <label>Mật khẩu mới (để trống nếu không đổi)</label>
                        <input v-model="form.password" type="password" placeholder="Để trống nếu không đổi" :disabled="isDetailMode"/>
                    </div>

                    <div class="modal-actions" v-if="!isDetailMode">
                        <button type="button" @click="closeModal" class="btn-cancel">Hủy</button>
                        <button type="submit" class="btn-save">
                            {{ isEditMode ? 'Cập nhật' : 'Thêm mới' }}
                        </button>
                    </div>
                </form>
            </div>
        </Teleport>

        <!-- Confirm xóa -->
        <Teleport to="body">
            <div v-if="showConfirm" class="modal-overlay" @click="showConfirm = false"></div>
            <div v-if="showConfirm" class="confirm-modal">
                <h3>Xác nhận xóa</h3>
                <p>Bạn có chắc chắn muốn xóa người dùng <strong>{{ userToDelete?.fullName }}</strong> không?</p>
                <div class="confirm-actions">
                    <button @click="deleteUser" class="btn-danger">Xóa vĩnh viễn</button>
                    <button @click="showConfirm = false" class="btn-cancel ">Hủy</button>
                </div>
            </div>
        </Teleport>
    </div>
    
</template>
<style scoped>
.form-row {
    display: flex;
    gap: 16px;
}

.form-row .form-group {
    flex: 1;
}

.user-info {
    display: flex;
    align-items: center;
    gap: 10px;
}

.avatar {
    width: 36px;
    height: 36px;
    border-radius: 50%;
    object-fit: cover;
}

.user-manager {
    padding: 24px;
    background: #f5f7fa;
    min-height: 100vh;
}

.header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 24px;
}

.header h2 {
    font-size: 32px;
    font-weight: 600;
    margin: 0;
    color: #2c3e50;
}

.btn-add {
    background: #2874f0;
    color: white;
    padding: 10px 20px;
    border: none;
    border-radius: 8px;
    font-weight: bold;
    cursor: pointer;
}
.modal-header{
    background: #ffffff1f;
    color: #333;
    padding: 16px 20px;
    display: flex;
    justify-content: space-between;
    align-items: center;
    border-radius: 16px 16px 0 0;
    border-bottom: 2px solid #eee;
}
.btn-close {
    /* background: none; */
    border: none;
    color: white;
    font-size: 28px;
    cursor: pointer;
}
</style>