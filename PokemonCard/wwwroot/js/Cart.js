// ==========================================
// 購物車 JavaScript
// ==========================================


// ==========================================
// 頁面載入
// ==========================================

document.addEventListener("DOMContentLoaded", function () {

    // 取得目前購物車數量
    updateCartCount();

});


// ==========================================
// 取得購物車數量
// ==========================================

function updateCartCount() {

    fetch("/Cart/GetCartCount")
        .then(response => response.json())
        .then(data => {

            if (data.success) {

                updateCartBadge(data.cartCount);

            }

        })
        .catch(error => {

            console.error(
                "取得購物車數量失敗：",
                error
            );

        });

}


// ==========================================
// 更新右上角紅點
// ==========================================

function updateCartBadge(count) {

    const cartCount =
        document.getElementById("cartCount");


    if (!cartCount) {
        return;
    }


    // 更新數字
    cartCount.textContent = count;


    // 沒有商品 → 隱藏
    if (count <= 0) {

        cartCount.classList.add("hidden");

    }
    else {

        // 有商品 → 顯示
        cartCount.classList.remove("hidden");

    }

}


// ==========================================
// 加入購物車
// ==========================================

function addToCart(
    productId,
    specificationId,
    quantity
) {

    const formData =
        new URLSearchParams();


    formData.append(
        "productId",
        productId
    );


    formData.append(
        "specificationId",
        specificationId
    );


    formData.append(
        "quantity",
        quantity
    );


    fetch("/Cart/Add", {

        method: "POST",

        headers: {
            "Content-Type":
                "application/x-www-form-urlencoded"
        },

        body: formData

    })
        .then(response => response.json())
        .then(data => {

            if (data.success) {

                // 更新右上角數量
                updateCartBadge(
                    data.cartCount
                );


                // 顯示加入購物車小視窗
                showCartToast();

            }

        })
        .catch(error => {

            console.error(
                "加入購物車失敗：",
                error
            );

        });

}


// ==========================================
// 顯示購物車 Toast
// ==========================================

function showCartToast() {

    const toast =
        document.getElementById("cartToast");


    if (!toast) {
        return;
    }


    // 顯示
    toast.classList.add("show");


    // 5 秒後自動關閉
    clearTimeout(
        window.cartToastTimer
    );


    window.cartToastTimer =
        setTimeout(function () {

            closeCartToast();

        }, 5000);

}


// ==========================================
// 關閉購物車 Toast
// ==========================================

function closeCartToast() {

    const toast =
        document.getElementById("cartToast");


    if (!toast) {
        return;
    }


    toast.classList.remove("show");

}
