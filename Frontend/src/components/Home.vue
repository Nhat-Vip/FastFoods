<script setup>
import { onMounted, ref } from 'vue';
import FastFood from './FastFood.vue';
import api from '@/api';
var categorySelected = ref("All")
const showFilterModal = ref(false);

const filters = ref({
    name: "",
    categoryId: "",
    minPrice: null,
    maxPrice: null
});

const openFilterModal = () => {
    categorySelected.value = "Lọc";
    showFilterModal.value = true;
};

const closeFilterModal = () => {
    showFilterModal.value = false;
};

// Reset filter
const resetFilters = () => {
    filters.value = {
        name: "",
        categoryId: "",
        minPrice: null,
        maxPrice: null
    };
};


const foods = ref([]);
const combos = ref([]);
const categories = ref([]);
const filterFoods = ref([]);
const filterCombos = ref([]);
const notFound = ref('');

const fetchFood = async () => {
    try {
        const res = await api.get("/fastfoods");
        foods.value = res.data;
        console.log("Food: ", res.data);
    }
    catch (e) {
        console.log("Có lỗi xảy ra khi tải dữ liệu food: ", e);
    }
}
const fetchCombo = async () => {
    try {
        const res = await api.get("/combos");
        combos.value = [...res.data];
        console.log("Combo clone:", [...combos.value]);

    }
    catch (e) {
        console.log("Có lỗi xảy ra khi tải dữ liệu combo: ", e);
    }
}
const fecthCategoies = async () => {
    try {
        const res = await api.get("/categories");
        categories.value = res.data;
        console.log("category: ", res.data);
    }
    catch (e) {
        console.log("Có lỗi xảy ra khi tải dữ liệu category: ", e);
    }
}

const FilterByCategory = (categoryId) => {
    console.log("filter: ",categoryId)
    if (categoryId == 0) {
        console.log("Food value: ", foods.value);
        filterFoods.value = [...foods.value];
        filterCombos.value = [...combos.value];
    }
    else if (categoryId == "combo") {
        filterFoods.value = [];
        if (combos.value.length == 0) {
            notFound.value = "Không có combo nào tồn tại"
            return;
        }
        filterCombos.value = [...combos.value];
    }
    else {
        filterFoods.value = foods.value.filter(item => item.categoryId == categoryId);
        filterCombos.value = combos.value.filter(item => item.comboItems.some(c => c.fastFood.categoryId == categoryId))

        if (filterFoods.value.length == 0 && filterCombos.value.length == 0) {
            notFound.value = "Không có sản phẩm nào thuộc danh mục " + categorySelected.value;
            return;
        }
    }
    notFound.value = '';
}

const applyFilters = () => {
    filterFoods.value = foods.value.filter(item => {

        const matchName =
            !filters.value.name ||
            item.name.toLowerCase().includes(filters.value.name.toLowerCase());

        const matchCategory =
            !filters.value.categoryId ||
            item.categoryId == filters.value.categoryId;

        const matchMinPrice =
            !filters.value.minPrice ||
            item.price >= filters.value.minPrice;

        const matchMaxPrice =
            !filters.value.maxPrice ||
            item.price <= filters.value.maxPrice;

        return matchName && matchCategory && matchMinPrice && matchMaxPrice;
    });
    filterCombos.value = combos.value.filter(item => {

        const matchName =
            !filters.value.name ||
            item.name.toLowerCase().includes(filters.value.name.toLowerCase());

        const matchCategory =
            !filters.value.categoryId ||
            item.comboItems.some( c=> c.fastFood.categoryId == filters.value.categoryId);

        const matchMinPrice =
            !filters.value.minPrice ||
            item.price >= filters.value.minPrice;

        const matchMaxPrice =
            !filters.value.maxPrice ||
            item.price <= filters.value.maxPrice;

        return matchName && matchCategory && matchMinPrice && matchMaxPrice;
    });

   
    if (filterFoods.value.length == 0 && filterCombos.value.length == 0) {
        notFound.value = "Không có sản phẩm nào được tìm thấy";
        showFilterModal.value = false;
        return;
    }
    notFound.value = '';
    showFilterModal.value = false;
};


onMounted(async () => {
    await fecthCategoies();
    await fetchFood();
    await fetchCombo();
    FilterByCategory(0);
})

</script>
<template>
    <div class="container-fluid">
        <div class="category">
            <ul class="d-flex">
                <li :class='["category-item", categorySelected == "All" ? "active" : ""]'
                    @click="categorySelected = 'All', FilterByCategory(0)">Tất cả</li>
                <li :class='["category-item", categorySelected == "Combo" ? "active" : ""]'
                    @click="categorySelected = 'Combo', FilterByCategory('combo')">Combo</li>
                <li v-for="item in categories"
                    :class='["category-item", categorySelected == item.categoryName ? "active" : ""]'
                    @click="categorySelected = item.categoryName, FilterByCategory(item.categoryId)">{{
                    item.categoryName }}</li>
                <!-- <li :class='["category-item", categorySelected == "Combo" ? "active" : ""]'
                    @click="categorySelected = 'Combo'">Combo</li> -->
                <li :class='["category-item", categorySelected == "Lọc" ? "active" : ""]' @click="openFilterModal()">Lọc
                </li>
            </ul>
        </div>
        <div class="line">
            <span class="line-text">Danh sách sản phẩm</span>
        </div>
           <div class="fs-4 fw-bold text-center mt-3" style="color: orangered;"
                 v-if="notFound">
                   {{notFound}}
            </div>
        <div class="list-products">
            <FastFood v-for="food in filterFoods.slice(0,8)" :imgUrl=food.imageUrl :foodId=food.fastFoodId :name=food.name
                :price="Number(food.price)"></FastFood>
            <FastFood v-for="combo in filterCombos.slice(0,8)" :imgUrl=combo.imageUrl :foodId=combo.comboId :name=combo.name
                :price="Number(combo.price)" :isCombo=true></FastFood>
            <!-- <FastFood imgUrl="1.png" foodId="1" name="Burger Bò Phô Mai" price="69000" />
            <FastFood imgUrl="1.png" foodId="1" name="Burger Bò Phô Mai" price="69000" />
            <FastFood imgUrl="1.png" foodId="1" name="Burger Bò Phô Mai" price="69000" />
            <FastFood imgUrl="1.png" foodId="1" name="Burger Bò Phô Mai" price="69000" />
            <FastFood imgUrl="1.png" foodId="1" name="Burger Bò Phô Mai" price="69000" />
            <FastFood imgUrl="1.png" foodId="1" name="Burger Bò Phô Mai" price="69000" /> -->
        </div>
        <div class="modal fade" tabindex="-1" :class="{ show: showFilterModal }"
            :style="{ display: showFilterModal ? 'block' : 'none' }" @click.self="closeFilterModal">
            <div class="modal-dialog modal-dialog-centered">
                <div class="modal-content">

                    <!-- Header -->
                    <div class="modal-header">
                        <h5 class="modal-title">Bộ lọc sản phẩm</h5>
                        <button type="button" class="btn-close" @click="closeFilterModal"></button>
                    </div>

                    <!-- Body -->
                    <div class="modal-body">

                        <!-- Theo tên -->
                        <div class="mb-3">
                            <label class="form-label">Tên sản phẩm</label>
                            <input v-model="filters.name" type="text" class="form-control"
                                placeholder="Nhập tên cần tìm..." />
                        </div>

                        <!-- Phân loại -->
                        <div class="mb-3">
                            <label class="form-label">Phân loại</label>
                            <select v-model="filters.categoryId" class="form-select">
                                <option value="">Tất cả</option>
                                <option v-for="c in categories" :key="c.categoryId" :value="c.categoryId">
                                    {{ c.categoryName }}
                                </option>
                            </select>
                        </div>

                        <!-- Giá -->
                        <div class="mb-3">
                            <label class="form-label">Khoảng giá</label>
                            <div class="d-flex gap-2">
                                <input v-model.number="filters.minPrice" type="number" class="form-control"
                                    placeholder="Min" />
                                <input v-model.number="filters.maxPrice" type="number" class="form-control"
                                    placeholder="Max" />
                            </div>
                        </div>

                    </div>

                    <!-- Footer -->
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="resetFilters">Reset</button>
                        <button class="btn btn-primary" @click="applyFilters">Áp dụng</button>
                    </div>

                </div>
            </div>
        </div>

        <!-- BACKDROP -->
        <div v-if="showFilterModal" class="modal-backdrop fade show">
        </div>
    </div>
</template>
<style scoped>
    .category{
        display: flex;
        justify-content: center;
        align-items: center;
        margin-top: 20px;
    }
    .category-item{
        list-style: none;
        padding: 8px 14px;
        border-radius: 16px;
        border: 2px solid #e0e0e0;
        background-color: #f8f9fa;
        font-size: 16px;
        cursor: pointer;
        font-weight: 600;
        color: #333;
        margin-right: 10px;
        transition: all 0.3s ease;

    }
    .category-item:hover{
        background: #f8f9fa;
        /* border-color: #ff6b35; */
        color: #ff6b35;
        transform: translateY(-2px);
        /* box-shadow: 0 10px 25px rgba(255, 107, 53, 0.2); */
    }
    .category-item.active {
        background: #ff6b35;
        color: white;
        border-color: #ff6b35;
        box-shadow: 0 8px 20px rgba(255, 107, 53, 0.4);
        transform: translateY(-2px);
    }
    .category-item.active:hover{
        border-color: #ff6b35;
        color: white;
    }
    .list-products{
        display: grid;
        width: 90%;
        padding: 5px 20px;
        grid-template-columns: 1fr 1fr 1fr 1fr;
        gap: 10px;
        flex-wrap: wrap;
        margin: 0 auto;
    }
    .line{
        position: relative;
        text-align: center;
        width: 80%;
        margin: 20px auto;
    }
    .line::after{
        content: "";
        position: absolute;
        top: 50%;
        left: 0;
        transform: translateY(-50%);
        width: 100%;
        height: 1px;
        background-color: #333;
        border-radius: 2px;
    }
    .line-text{
        position: relative;
        font-size: 28px;
        font-weight: 700;
        background-color: #f5f5f5;
        padding: 0 15px;
        z-index: 1;
    }
</style>