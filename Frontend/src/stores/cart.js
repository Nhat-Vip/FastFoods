import { defineStore } from "pinia";
import { ref ,watch} from "vue";

export const useCartStore = defineStore("cart", () => {
    const isOrder = ref(false);
    const cartItem = ref(JSON.parse(localStorage.getItem("cart")) || []);
    const cartOrder = ref(JSON.parse(localStorage.getItem("cartOrder")) || []);

    function addCart(item, isCart) {
        if (!isCart) {
            cartOrder.value = [];
            cartOrder.value.push(item);
            isOrder.value = true;
        }
        else {        
            const isFood = cartItem.value.find(i => i.id == item.id && i.isCombo == item.isCombo);
            if (isFood) {
                isFood.quantity += 1;
                return;
            }
            isOrder.value = false;
            cartItem.value.push(item);
        }
    }
    function removeCart(id, isCombo, isCart) {
        if (isCart) {
            cartItem.value = cartItem.value.filter(item => !(item.id == id && item.isCombo == isCombo))
        }
        else {
            cartOrder.value = cartOrder.value.filter(item => !(item.id == id && item.isCombo == isCombo))
        }
    }
    function OrderWith(isCart) {
        isOrder.value = !isCart;
    }
    function resetCart() {
        cartItem.value = [];
        cartOrder.value = [];
    }
    watch(cartItem, (value) => {
        localStorage.setItem("cart", JSON.stringify(value));
    }, { deep: true });
    watch(cartOrder, (value) => {
        localStorage.setItem("cartOrder", JSON.stringify(value));
    }, { deep: true });

    return { cartItem, cartOrder, OrderWith, isOrder, addCart, removeCart, resetCart }
});