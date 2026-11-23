// ==========================================================================
// POS Application JavaScript Engine
// ==========================================================================

const PosApp = (function () {
    // State
    let cart = [];
    let currentCustomer = null;
    let redeemedPoints = 0;
    let ageConfirmed = false;
    let currentCategory = 0;
    let shiftInvoices = [];
    let activeDetailInvoiceId = null;

    // Elements
    const elements = {
        barcodeInput: document.getElementById('barcodeInput'),
        searchInput: document.getElementById('searchInput'),
        productGrid: document.getElementById('productGrid'),
        cartItemsList: document.getElementById('cartItemsList'),
        emptyCartMsg: document.getElementById('emptyCartMsg'),
        subTotalEl: document.getElementById('subTotalEl'),
        discountEl: document.getElementById('discountEl'),
        finalTotalEl: document.getElementById('finalTotalEl'),
        badgeItemCount: document.getElementById('badgeItemCount'),
        phoneInput: document.getElementById('phoneInput'),
        customerInfoBox: document.getElementById('customerInfoBox'),
        customerNameEl: document.getElementById('customerNameEl'),
        customerPointsEl: document.getElementById('customerPointsEl'),
        redeemPointsInput: document.getElementById('redeemPointsInput'),
        ageWarningBanner: document.getElementById('ageWarningBanner'),
        // Modals
        ageModal: new bootstrap.Modal(document.getElementById('ageModal')),
        cashModal: new bootstrap.Modal(document.getElementById('cashModal')),
        cardModal: new bootstrap.Modal(document.getElementById('cardModal')),
        qrModal: new bootstrap.Modal(document.getElementById('qrModal')),
        successModal: new bootstrap.Modal(document.getElementById('successModal')),
        invoiceHistoryModal: document.getElementById('invoiceHistoryModal') ? new bootstrap.Modal(document.getElementById('invoiceHistoryModal')) : null
    };

    function init() {
        bindEvents();
        bindCardClicks();
        renderCart();
        if (elements.barcodeInput) elements.barcodeInput.focus();
    }

    function bindCardClicks() {
        document.querySelectorAll('.pos-product-card').forEach(card => {
            if (card.dataset.bound) return;
            card.dataset.bound = 'true';
            card.addEventListener('click', function () {
                const item = {
                    maHang: parseInt(this.dataset.id),
                    maVach: this.dataset.barcode,
                    tenHang: this.dataset.name,
                    donViTinh: this.dataset.unit,
                    donGia: parseFloat(this.dataset.price),
                    tonQuayHienTai: parseInt(this.dataset.stock),
                    gioiHanDoTuoi: parseInt(this.dataset.age || '0'),
                    giamGiaDonVi: parseFloat(this.dataset.discount || '0'),
                    tenKM: this.dataset.promoname,
                    maKM: this.dataset.promoid ? parseInt(this.dataset.promoid) : null
                };
                addToCart(item);
            });
        });
    }

    function bindEvents() {
        // Quét mã vạch khi nhấn Enter
        elements.barcodeInput?.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                const code = this.value.trim();
                if (code) {
                    scanBarcode(code);
                    this.value = '';
                }
            }
        });

        // Tìm kiếm sản phẩm
        let searchTimeout;
        elements.searchInput?.addEventListener('input', function () {
            clearTimeout(searchTimeout);
            const query = this.value.trim();
            searchTimeout = setTimeout(() => {
                searchProducts(query);
            }, 300);
        });

        // Lọc danh mục
        document.querySelectorAll('.pos-category-btn').forEach(btn => {
            btn.addEventListener('click', function () {
                document.querySelectorAll('.pos-category-btn').forEach(b => b.classList.remove('active'));
                this.classList.add('active');
                currentCategory = parseInt(this.dataset.category || '0');
                filterProductsByCategory(currentCategory);
            });
        });

        // Tìm khách hàng bằng số điện thoại
        document.getElementById('btnSearchCustomer')?.addEventListener('click', lookupCustomer);
        elements.phoneInput?.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                lookupCustomer();
            }
        });

        // Đổi điểm tích lũy
        elements.redeemPointsInput?.addEventListener('input', function () {
            let pts = parseInt(this.value) || 0;
            if (currentCustomer && pts > currentCustomer.diemTichLuy) {
                pts = currentCustomer.diemTichLuy;
                this.value = pts;
            }
            redeemedPoints = pts;
            renderCart();
        });

        // Nút mở modal xác nhận độ tuổi
        document.getElementById('btnConfirmAgeModal')?.addEventListener('click', function () {
            ageConfirmed = true;
            elements.ageModal.hide();
            renderCart();
        });

        // Tiền mặt: Quick chips
        document.querySelectorAll('.cash-chip').forEach(chip => {
            chip.addEventListener('click', function () {
                const val = parseInt(this.dataset.value);
                const cashInput = document.getElementById('cashReceivedInput');
                if (val === 0) {
                    // Đúng số tiền
                    cashInput.value = calculateFinalTotal();
                } else {
                    cashInput.value = val;
                }
                calculateCashChange();
            });
        });

        document.getElementById('cashReceivedInput')?.addEventListener('input', calculateCashChange);

        // Nút kích hoạt thanh toán
        document.getElementById('btnPayCash')?.addEventListener('click', () => initiatePayment('TIEN_MAT'));
        document.getElementById('btnPayQR')?.addEventListener('click', () => initiatePayment('QR'));
        document.getElementById('btnPayCard')?.addEventListener('click', () => initiatePayment('THE'));

        // Xác nhận thanh toán các phương thức
        document.getElementById('btnSubmitCashPay')?.addEventListener('click', submitCashPayment);
        document.getElementById('btnSubmitCardPay')?.addEventListener('click', submitCardPayment);
        document.getElementById('btnSubmitQRPay')?.addEventListener('click', submitQRPayment);

        // Nút xóa toàn bộ giỏ hàng
        document.getElementById('btnClearCart')?.addEventListener('click', function () {
            if (cart.length > 0 && confirm('Bạn có chắc chắn muốn xóa toàn bộ giỏ hàng?')) {
                clearCart();
            }
        });

        // Nút mở lịch sử hóa đơn trong ca
        document.getElementById('btnOpenHistoryModal')?.addEventListener('click', function () {
            openInvoiceHistoryModal();
        });

        // Tìm kiếm trong danh sách hóa đơn
        document.getElementById('historySearchInput')?.addEventListener('input', function () {
            filterHistoryInvoices(this.value.trim());
        });

        // Nút làm mới lịch sử
        document.getElementById('btnRefreshHistory')?.addEventListener('click', function () {
            loadShiftInvoices();
        });

        // Nút quay lại danh sách từ chi tiết
        document.getElementById('btnBackToInvoiceList')?.addEventListener('click', function () {
            document.getElementById('invoiceDetailView').style.display = 'none';
            document.getElementById('invoiceListView').style.display = 'block';
        });

        // Nút in lại hóa đơn trong chi tiết
        document.getElementById('btnPrintDetailInvoice')?.addEventListener('click', function () {
            if (activeDetailInvoiceId) {
                window.open(`/Pos/InHoaDon/${activeDetailInvoiceId}`, '_blank', 'width=450,height=650');
            }
        });

        // Phím tắt bàn phím
        window.addEventListener('keydown', function (e) {
            if (e.key === 'F2') {
                e.preventDefault();
                elements.barcodeInput?.focus();
            } else if (e.key === 'F3') {
                e.preventDefault();
                openInvoiceHistoryModal();
            } else if (e.key === 'F4') {
                e.preventDefault();
                elements.phoneInput?.focus();
            } else if (e.key === 'F8') {
                e.preventDefault();
                initiatePayment('TIEN_MAT');
            } else if (e.key === 'F9') {
                e.preventDefault();
                initiatePayment('QR');
            } else if (e.key === 'F10') {
                e.preventDefault();
                initiatePayment('THE');
            }
        });
    }

    // Scan Barcode API
    async function scanBarcode(barcode) {
        try {
            const res = await fetch(`/Pos/ScanBarcode?barcode=${encodeURIComponent(barcode)}`);
            const data = await res.json();
            if (data.success && data.data) {
                addToCart(data.data);
            } else {
                alert(data.message || 'Không tìm thấy sản phẩm.');
            }
        } catch (err) {
            console.error(err);
            alert('Lỗi kết nối khi quét mã vạch.');
        }
    }

    // Search Products API
    async function searchProducts(query) {
        try {
            const res = await fetch(`/Pos/Search?q=${encodeURIComponent(query || '')}`);
            const data = await res.json();
            if (data.success && data.data) {
                renderProductGrid(data.data);
                if (currentCategory > 0) {
                    filterProductsByCategory(currentCategory);
                }
            }
        } catch (err) {
            console.error(err);
        }
    }

    function renderProductGrid(products) {
        if (!elements.productGrid) return;
        elements.productGrid.innerHTML = '';

        if (products.length === 0) {
            elements.productGrid.innerHTML = '<div class="col-12 text-center text-muted py-5"><i class="fa-solid fa-box-open fa-3x mb-3 text-secondary"></i><br>Không tìm thấy sản phẩm nào</div>';
            return;
        }

        products.forEach(p => {
            const card = document.createElement('div');
            card.className = `pos-product-card ${p.gioiHanDoTuoi >= 18 ? 'restricted-item' : ''}`;
            card.dataset.id = p.maHang;
            card.dataset.category = p.maNhom;

            card.innerHTML = `
                <div>
                    <div class="d-flex justify-content-between align-items-start mb-1">
                        <small class="text-secondary font-monospace" style="font-size: 0.7rem;">${p.maVach}</small>
                        ${p.gioiHanDoTuoi >= 18 ? '<span class="badge bg-danger" style="font-size: 0.65rem;">18+</span>' : ''}
                    </div>
                    <div class="pos-product-img-wrap">
                        <img src="/images/products/${p.maHang}.svg" class="pos-product-img" alt="${p.tenHang}" onerror="this.onerror=null; this.src='/images/products/default.svg';" />
                    </div>
                    <div class="pos-product-title" title="${p.tenHang}">${p.tenHang}</div>
                </div>
                <div>
                    ${p.giamGiaDonVi > 0 ? `<div class="text-danger small text-decoration-line-through">${p.donGia.toLocaleString('vi-VN')}đ</div>` : ''}
                    <div class="d-flex justify-content-between align-items-baseline">
                        <div class="pos-product-price">${(p.donGia - p.giamGiaDonVi).toLocaleString('vi-VN')}đ</div>
                        <div class="pos-product-stock">Quầy: <strong>${p.tonQuayHienTai}</strong> ${p.donViTinh}</div>
                    </div>
                </div>
            `;

            card.addEventListener('click', () => addToCart(p));
            elements.productGrid.appendChild(card);
        });
    }

    function filterProductsByCategory(categoryId) {
        document.querySelectorAll('.pos-product-card').forEach(card => {
            if (categoryId === 0 || card.dataset.category === categoryId.toString()) {
                card.style.display = 'flex';
            } else {
                card.style.display = 'none';
            }
        });
    }

    // Add to Cart
    function addToCart(product) {
        const existing = cart.find(item => item.maHang === product.maHang);
        if (existing) {
            if (existing.soLuong >= product.tonQuayHienTai) {
                alert(`Số lượng trên quầy chỉ còn ${product.tonQuayHienTai} ${product.donViTinh}. Vui lòng bổ sung thêm hàng từ kho!`);
                return;
            }
            existing.soLuong++;
        } else {
            if (product.tonQuayHienTai <= 0) {
                alert(`Sản phẩm '${product.tenHang}' hiện không còn tồn trên quầy!`);
                return;
            }
            cart.push({
                maHang: product.maHang,
                maVach: product.maVach,
                tenHang: product.tenHang,
                donViTinh: product.donViTinh,
                donGia: product.donGia,
                soLuong: 1,
                gioiHanDoTuoi: product.gioiHanDoTuoi,
                maKM: product.maKM,
                tenKM: product.tenKM,
                giamGiaDonVi: product.giamGiaDonVi || 0,
                tonQuayHienTai: product.tonQuayHienTai
            });
        }

        renderCart();
    }

    function updateQuantity(maHang, delta) {
        const item = cart.find(i => i.maHang === maHang);
        if (!item) return;

        const newQty = item.soLuong + delta;
        if (newQty <= 0) {
            removeItem(maHang);
            return;
        }

        if (newQty > item.tonQuayHienTai) {
            alert(`Tồn quầy hiện tại chỉ còn ${item.tonQuayHienTai} ${item.donViTinh}!`);
            return;
        }

        item.soLuong = newQty;
        renderCart();
    }

    function removeItem(maHang) {
        cart = cart.filter(i => i.maHang !== maHang);
        renderCart();
    }

    function clearCart() {
        cart = [];
        redeemedPoints = 0;
        ageConfirmed = false;
        if (elements.redeemPointsInput) elements.redeemPointsInput.value = '0';
        renderCart();
    }

    // Render Cart HTML
    function renderCart() {
        if (!elements.cartItemsList) return;

        elements.cartItemsList.innerHTML = '';
        const totalItems = cart.reduce((sum, item) => sum + item.soLuong, 0);
        if (elements.badgeItemCount) elements.badgeItemCount.innerText = totalItems;

        if (cart.length === 0) {
            if (elements.emptyCartMsg) elements.emptyCartMsg.style.display = 'block';
            if (elements.ageWarningBanner) elements.ageWarningBanner.style.display = 'none';
        } else {
            if (elements.emptyCartMsg) elements.emptyCartMsg.style.display = 'none';

            // Kiểm tra hàng 18+
            const hasRestricted = cart.some(i => i.gioiHanDoTuoi >= 18);
            if (elements.ageWarningBanner) {
                if (hasRestricted) {
                    elements.ageWarningBanner.style.display = 'block';
                    elements.ageWarningBanner.className = ageConfirmed 
                        ? 'alert alert-success py-2 px-3 small d-flex align-items-center justify-content-between mb-2' 
                        : 'alert alert-danger py-2 px-3 small d-flex align-items-center justify-content-between mb-2';
                    elements.ageWarningBanner.innerHTML = ageConfirmed
                        ? '<div><i class="fa-solid fa-check-circle me-1"></i> <strong>Đã xác nhận độ tuổi 18+</strong></div>'
                        : '<div><i class="fa-solid fa-triangle-exclamation me-1"></i> <strong>Có hàng 18+! Cần xác nhận độ tuổi</strong></div><button class="btn btn-sm btn-danger py-0 px-2" onclick="PosApp.showAgeModal()">Xác nhận</button>';
                } else {
                    elements.ageWarningBanner.style.display = 'none';
                }
            }

            cart.forEach(item => {
                const itemTotal = (item.donGia - item.giamGiaDonVi) * item.soLuong;
                const div = document.createElement('div');
                div.className = 'pos-cart-item';
                div.innerHTML = `
                    <div style="flex: 1; padding-right: 0.5rem;">
                        <div class="fw-semibold text-dark text-truncate" style="max-width: 220px;" title="${item.tenHang}">
                            ${item.gioiHanDoTuoi >= 18 ? '<span class="badge bg-danger me-1" style="font-size: 0.6rem;">18+</span>' : ''}
                            ${item.tenHang}
                        </div>
                        <div class="small text-muted">
                            ${item.donGia.toLocaleString('vi-VN')}đ / ${item.donViTinh}
                            ${item.giamGiaDonVi > 0 ? `<span class="badge bg-success-subtle text-success ms-1">Giảm -${item.giamGiaDonVi.toLocaleString('vi-VN')}đ</span>` : ''}
                        </div>
                    </div>
                    <div class="d-flex align-items-center gap-2">
                        <button class="btn btn-outline-secondary pos-qty-btn" onclick="PosApp.updateQuantity(${item.maHang}, -1)"><i class="fa-solid fa-minus"></i></button>
                        <span class="fw-bold font-monospace px-1" style="min-width: 24px; text-align: center;">${item.soLuong}</span>
                        <button class="btn btn-outline-secondary pos-qty-btn" onclick="PosApp.updateQuantity(${item.maHang}, 1)"><i class="fa-solid fa-plus"></i></button>
                    </div>
                    <div class="text-end ms-3" style="min-width: 80px;">
                        <div class="fw-bold text-dark">${itemTotal.toLocaleString('vi-VN')}đ</div>
                        <a href="javascript:void(0)" class="text-danger small" onclick="PosApp.removeItem(${item.maHang})"><i class="fa-solid fa-trash-can"></i></a>
                    </div>
                `;
                elements.cartItemsList.appendChild(div);
            });
        }

        updateTotals();
    }

    function calculateFinalTotal() {
        const rawTotal = cart.reduce((sum, item) => sum + (item.donGia * item.soLuong), 0);
        const promoDiscount = cart.reduce((sum, item) => sum + (item.giamGiaDonVi * item.soLuong), 0);
        const pointDiscount = redeemedPoints * 1000;
        const totalDiscount = promoDiscount + pointDiscount;
        return Math.max(0, rawTotal - totalDiscount);
    }

    function updateTotals() {
        const rawTotal = cart.reduce((sum, item) => sum + (item.donGia * item.soLuong), 0);
        const promoDiscount = cart.reduce((sum, item) => sum + (item.giamGiaDonVi * item.soLuong), 0);
        const pointDiscount = redeemedPoints * 1000;
        const totalDiscount = promoDiscount + pointDiscount;
        const finalTotal = Math.max(0, rawTotal - totalDiscount);

        if (elements.subTotalEl) elements.subTotalEl.innerText = rawTotal.toLocaleString('vi-VN') + 'đ';
        if (elements.discountEl) elements.discountEl.innerText = '-' + totalDiscount.toLocaleString('vi-VN') + 'đ';
        if (elements.finalTotalEl) elements.finalTotalEl.innerText = finalTotal.toLocaleString('vi-VN') + 'đ';
    }

    // Customer Lookup
    async function lookupCustomer() {
        const phone = elements.phoneInput?.value.trim();
        if (!phone) return;

        try {
            const res = await fetch(`/Pos/Customer?phone=${encodeURIComponent(phone)}`);
            const data = await res.json();
            if (data.success && data.data) {
                currentCustomer = data.data;
                if (elements.customerInfoBox) elements.customerInfoBox.style.display = 'block';
                if (elements.customerNameEl) elements.customerNameEl.innerText = currentCustomer.hoTen;
                if (elements.customerPointsEl) elements.customerPointsEl.innerText = currentCustomer.diemTichLuy.toLocaleString('vi-VN');
            } else {
                if (confirm(`Chưa có khách hàng với số điện thoại ${phone}. Bạn có muốn tạo mới không?`)) {
                    const name = prompt('Nhập họ tên khách hàng:');
                    if (name) {
                        createCustomer(name, phone);
                    }
                }
            }
        } catch (err) {
            console.error(err);
        }
    }

    async function createCustomer(name, phone) {
        try {
            const res = await fetch('/Pos/CreateCustomer', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ hoTen: name, soDienThoai: phone })
            });
            const data = await res.json();
            if (data.success && data.data) {
                currentCustomer = data.data;
                if (elements.customerInfoBox) elements.customerInfoBox.style.display = 'block';
                if (elements.customerNameEl) elements.customerNameEl.innerText = currentCustomer.hoTen;
                if (elements.customerPointsEl) elements.customerPointsEl.innerText = currentCustomer.diemTichLuy.toLocaleString('vi-VN');
                alert('Tạo thông tin khách hàng thành công!');
            }
        } catch (err) {
            console.error(err);
        }
    }

    // Payment Flow
    function initiatePayment(method) {
        if (cart.length === 0) {
            alert('Giỏ hàng đang trống! Vui lòng chọn hoặc quét sản phẩm trước.');
            return;
        }

        // Kiểm tra xác nhận độ tuổi nếu có hàng 18+
        const hasRestricted = cart.some(i => i.gioiHanDoTuoi >= 18);
        if (hasRestricted && !ageConfirmed) {
            elements.ageModal.show();
            return;
        }

        const finalTotal = calculateFinalTotal();

        if (method === 'TIEN_MAT') {
            document.getElementById('modalCashNeedPay').innerText = finalTotal.toLocaleString('vi-VN') + 'đ';
            const cashInput = document.getElementById('cashReceivedInput');
            cashInput.value = finalTotal;
            calculateCashChange();
            elements.cashModal.show();
            setTimeout(() => cashInput.select(), 300);
        } else if (method === 'QR') {
            document.getElementById('modalQRNeedPay').innerText = finalTotal.toLocaleString('vi-VN') + 'đ';
            document.getElementById('qrCodeText').innerText = `Thanh toán HĐ ${finalTotal.toLocaleString('vi-VN')}đ`;
            elements.qrModal.show();
        } else if (method === 'THE') {
            document.getElementById('modalCardNeedPay').innerText = finalTotal.toLocaleString('vi-VN') + 'đ';
            elements.cardModal.show();
        }
    }

    function calculateCashChange() {
        const finalTotal = calculateFinalTotal();
        const cashReceived = parseFloat(document.getElementById('cashReceivedInput')?.value || '0');
        const change = cashReceived - finalTotal;

        const changeEl = document.getElementById('cashChangeAmount');
        const submitBtn = document.getElementById('btnSubmitCashPay');

        if (change >= 0) {
            changeEl.innerText = change.toLocaleString('vi-VN') + 'đ';
            changeEl.className = 'text-success fw-bold fs-4 font-monospace';
            if (submitBtn) submitBtn.disabled = false;
        } else {
            changeEl.innerText = 'Thiếu ' + Math.abs(change).toLocaleString('vi-VN') + 'đ';
            changeEl.className = 'text-danger fw-bold fs-4 font-monospace';
            if (submitBtn) submitBtn.disabled = true;
        }
    }

    // Submit Payments
    async function submitCashPayment() {
        const cashReceived = parseFloat(document.getElementById('cashReceivedInput').value || '0');
        const payload = buildCheckoutPayload('TIEN_MAT');
        payload.tienKhachDua = cashReceived;
        await executeCheckout(payload, elements.cashModal);
    }

    async function submitCardPayment() {
        const last4 = document.getElementById('cardLast4Digits').value.trim() || '8888';
        const traceCode = document.getElementById('cardTraceCode').value.trim() || 'AUTH' + Math.floor(Math.random() * 900000 + 100000);
        const payload = buildCheckoutPayload('THE');
        payload.soTheCuoi = last4;
        payload.maChuanChi = traceCode;
        await executeCheckout(payload, elements.cardModal);
    }

    async function submitQRPayment() {
        const txId = document.getElementById('qrTxId').value.trim() || 'VQR' + Date.now();
        const payload = buildCheckoutPayload('QR');
        payload.maGiaoDichQR = txId;
        payload.loaiVi = 'VietQR';
        await executeCheckout(payload, elements.qrModal);
    }

    function buildCheckoutPayload(method) {
        return {
            phuongThuc: method,
            maKH: currentCustomer ? currentCustomer.maKH : null,
            diemDoi: redeemedPoints,
            daXacNhanDoTuoi: ageConfirmed,
            items: cart
        };
    }

    async function executeCheckout(payload, modalToHide) {
        try {
            const btn = document.activeElement;
            if (btn) btn.disabled = true;

            const res = await fetch('/Pos/Checkout', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            const result = await res.json();
            if (modalToHide) modalToHide.hide();

            if (result.success) {
                // Hiển thị modal thành công
                document.getElementById('successInvoiceCode').innerText = result.soHoaDon;
                document.getElementById('successFinalAmount').innerText = result.thanhTien.toLocaleString('vi-VN') + 'đ';
                document.getElementById('successChangeAmount').innerText = result.tienThoi.toLocaleString('vi-VN') + 'đ';
                
                const btnPrint = document.getElementById('btnPrintInvoiceModal');
                btnPrint.onclick = function () {
                    window.open(`/Pos/InHoaDon/${result.maHoaDon}`, '_blank', 'width=450,height=650');
                };

                elements.successModal.show();
                clearCart();
            } else {
                alert('Lỗi thanh toán: ' + result.message);
            }
        } catch (err) {
            console.error(err);
            alert('Lỗi kết nối máy chủ khi xử lý thanh toán.');
        }
    }

    // ===== CHỨC NĂNG LỊCH SỬ HÓA ĐƠN TRONG CA =====
    function openInvoiceHistoryModal() {
        if (!elements.invoiceHistoryModal) return;
        document.getElementById('invoiceDetailView').style.display = 'none';
        document.getElementById('invoiceListView').style.display = 'block';
        elements.invoiceHistoryModal.show();
        loadShiftInvoices();
    }

    async function loadShiftInvoices() {
        const tbody = document.getElementById('historyTableBody');
        if (!tbody) return;
        tbody.innerHTML = `
            <tr>
                <td colspan="8" class="text-center py-4 text-muted">
                    <i class="fa-solid fa-spinner fa-spin me-1"></i> Đang tải dữ liệu hóa đơn...
                </td>
            </tr>`;

        try {
            const res = await fetch('/Pos/GetInvoicesByCa');
            const data = await res.json();
            if (data.success && Array.isArray(data.data)) {
                shiftInvoices = data.data;
                const totalCount = shiftInvoices.length;
                const totalRevenue = shiftInvoices
                    .filter(i => i.trangThai === 'HOAN_TAT')
                    .reduce((sum, i) => sum + (i.thanhTien || (i.tongTien - i.giamGia)), 0);

                const countEl = document.getElementById('historyTotalCount');
                const revEl = document.getElementById('historyTotalRevenue');
                const titleEl = document.getElementById('invoiceHistoryModalTitle');
                if (countEl) countEl.innerText = totalCount;
                if (revEl) revEl.innerText = totalRevenue.toLocaleString('vi-VN') + 'đ';
                if (titleEl && data.maCa) {
                    titleEl.innerText = `Lịch Sử Hóa Đơn Ca #${data.maCa}`;
                }

                renderHistoryTable(shiftInvoices);
            } else {
                tbody.innerHTML = `
                    <tr>
                        <td colspan="8" class="text-center py-4 text-danger">
                            ${data.message || 'Không thể tải lịch sử hóa đơn.'}
                        </td>
                    </tr>`;
            }
        } catch (err) {
            console.error(err);
            tbody.innerHTML = `
                <tr>
                    <td colspan="8" class="text-center py-4 text-danger">
                        Lỗi kết nối khi tải danh sách hóa đơn.
                    </td>
                </tr>`;
        }
    }

    function renderHistoryTable(invoices) {
        const tbody = document.getElementById('historyTableBody');
        if (!tbody) return;

        if (!invoices || invoices.length === 0) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="8" class="text-center py-4 text-muted">
                        <i class="fa-solid fa-receipt fa-2x mb-2 text-secondary d-block"></i>
                        Chưa có hóa đơn nào được lập trong ca làm việc này.
                    </td>
                </tr>`;
            return;
        }

        tbody.innerHTML = invoices.map(hd => {
            const dateStr = hd.ngayGio ? new Date(hd.ngayGio).toLocaleString('vi-VN') : '--';
            const customerStr = hd.tenKhachHang ? `${hd.tenKhachHang} (${hd.soDienThoaiKH || ''})` : '<span class="text-muted">Khách vãng lai</span>';
            const statusBadge = hd.trangThai === 'HOAN_TAT'
                ? '<span class="badge bg-success-subtle text-success border border-success">Hoàn tất</span>'
                : '<span class="badge bg-danger-subtle text-danger border border-danger">Đã hủy</span>';
            const thanhTien = hd.thanhTien || (hd.tongTien - hd.giamGia);

            return `
                <tr>
                    <td class="ps-3 font-monospace fw-bold text-primary">
                        <a href="javascript:void(0)" class="text-decoration-none view-detail-link" data-id="${hd.maHoaDon}">
                            ${hd.soHoaDon || 'HD-' + hd.maHoaDon}
                        </a>
                    </td>
                    <td class="small text-secondary">${dateStr}</td>
                    <td class="small">${customerStr}</td>
                    <td class="text-end font-monospace">${(hd.tongTien || 0).toLocaleString('vi-VN')}đ</td>
                    <td class="text-end font-monospace text-danger">-${(hd.giamGia || 0).toLocaleString('vi-VN')}đ</td>
                    <td class="text-end font-monospace fw-bold text-success">${thanhTien.toLocaleString('vi-VN')}đ</td>
                    <td class="text-center">${statusBadge}</td>
                    <td class="text-center pe-3">
                        <div class="btn-group btn-group-sm">
                            <button type="button" class="btn btn-outline-primary py-1 px-2 btn-view-invoice-detail" data-id="${hd.maHoaDon}" title="Xem chi tiết các mặt hàng">
                                <i class="fa-solid fa-eye me-1"></i> Chi tiết
                            </button>
                            <button type="button" class="btn btn-outline-secondary py-1 px-2 btn-reprint-invoice" data-id="${hd.maHoaDon}" title="In lại hóa đơn">
                                <i class="fa-solid fa-print"></i>
                            </button>
                        </div>
                    </td>
                </tr>`;
        }).join('');

        // Gán sự kiện click xem chi tiết
        tbody.querySelectorAll('.btn-view-invoice-detail, .view-detail-link').forEach(btn => {
            btn.addEventListener('click', function () {
                const id = parseInt(this.dataset.id);
                viewInvoiceDetail(id);
            });
        });

        // Gán sự kiện click in lại
        tbody.querySelectorAll('.btn-reprint-invoice').forEach(btn => {
            btn.addEventListener('click', function () {
                const id = parseInt(this.dataset.id);
                window.open(`/Pos/InHoaDon/${id}`, '_blank', 'width=450,height=650');
            });
        });
    }

    function filterHistoryInvoices(query) {
        if (!query) {
            renderHistoryTable(shiftInvoices);
            return;
        }
        const q = query.toLowerCase();
        const filtered = shiftInvoices.filter(i => {
            const code = (i.soHoaDon || '').toLowerCase();
            const cusName = (i.tenKhachHang || '').toLowerCase();
            const cusPhone = (i.soDienThoaiKH || '').toLowerCase();
            return code.includes(q) || cusName.includes(q) || cusPhone.includes(q);
        });
        renderHistoryTable(filtered);
    }

    async function viewInvoiceDetail(maHoaDon) {
        try {
            const res = await fetch(`/Pos/GetInvoiceDetail?id=${maHoaDon}`);
            const data = await res.json();
            if (!data.success || !data.data) {
                alert(data.message || 'Không tìm thấy chi tiết hóa đơn.');
                return;
            }

            const hd = data.data;
            activeDetailInvoiceId = hd.maHoaDon;

            document.getElementById('detailInvoiceCode').innerText = hd.soHoaDon || 'HD-' + hd.maHoaDon;
            document.getElementById('detailInvoiceTime').innerText = hd.ngayGio ? new Date(hd.ngayGio).toLocaleString('vi-VN') : '--';
            document.getElementById('detailCashierName').innerText = hd.tenThuNgan || '--';
            document.getElementById('detailCustomerInfo').innerText = hd.tenKhachHang ? `${hd.tenKhachHang} - ${hd.soDienThoaiKH || ''}` : 'Khách vãng lai';

            // Render danh sách mặt hàng
            const itemsTbody = document.getElementById('detailItemsTableBody');
            if (hd.chiTiet && hd.chiTiet.length > 0) {
                itemsTbody.innerHTML = hd.chiTiet.map((ct, idx) => {
                    const hsdStr = ct.hanSuDung ? new Date(ct.hanSuDung).toLocaleDateString('vi-VN') : '--';
                    const loHsd = ct.soLo ? `${ct.soLo} (${hsdStr})` : '--';
                    const thanhTien = ct.thanhTien || (ct.soLuong * ct.donGia - (ct.soTienGiam || 0));
                    return `
                        <tr>
                            <td class="ps-3 text-center text-muted">${idx + 1}</td>
                            <td class="fw-semibold text-dark">${ct.tenHang || 'Sản phẩm #' + ct.maHang}</td>
                            <td class="text-center small">${ct.donViTinh || ''}</td>
                            <td class="text-center font-monospace small text-muted">${loHsd}</td>
                            <td class="text-center fw-bold">${ct.soLuong}</td>
                            <td class="text-end font-monospace">${(ct.donGia || 0).toLocaleString('vi-VN')}đ</td>
                            <td class="text-end font-monospace text-danger">${(ct.soTienGiam || 0) > 0 ? '-' + ct.soTienGiam.toLocaleString('vi-VN') + 'đ' : '0đ'}</td>
                            <td class="text-end pe-3 font-monospace fw-bold">${thanhTien.toLocaleString('vi-VN')}đ</td>
                        </tr>`;
                }).join('');
            } else {
                itemsTbody.innerHTML = `
                    <tr>
                        <td colspan="8" class="text-center py-3 text-muted">Không có dữ liệu mặt hàng</td>
                    </tr>`;
            }

            // Render phương thức thanh toán
            const payBox = document.getElementById('detailPaymentMethodsList');
            if (hd.thanhToanList && hd.thanhToanList.length > 0) {
                payBox.innerHTML = hd.thanhToanList.map(tt => {
                    let typeName = 'Tiền mặt';
                    let typeIcon = 'fa-money-bill-wave text-success';
                    if (tt.loaiThanhToan === 'QR') {
                        typeName = 'Chuyển khoản QR';
                        typeIcon = 'fa-qrcode text-primary';
                    } else if (tt.loaiThanhToan === 'THE') {
                        typeName = 'Thẻ ngân hàng (POS)';
                        typeIcon = 'fa-credit-card text-info';
                    }

                    let extra = '';
                    if (tt.tienKhachDua != null && tt.tienKhachDua > 0) {
                        extra = `<div class="text-muted" style="font-size: 0.75rem;">Khách đưa: ${tt.tienKhachDua.toLocaleString('vi-VN')}đ - Thối lại: ${(tt.tienThoi || 0).toLocaleString('vi-VN')}đ</div>`;
                    }
                    if (tt.maGiaoDich) {
                        extra += `<div class="text-muted font-monospace" style="font-size: 0.72rem;">Mã GD: ${tt.maGiaoDich}</div>`;
                    }

                    return `
                        <div class="d-flex justify-content-between align-items-center py-1 border-bottom">
                            <div>
                                <i class="fa-solid ${typeIcon} me-1"></i> <strong>${typeName}</strong>
                                ${extra}
                            </div>
                            <span class="font-monospace fw-bold text-dark">${(tt.soTien || 0).toLocaleString('vi-VN')}đ</span>
                        </div>`;
                }).join('');
            } else {
                payBox.innerHTML = '<span class="text-muted">Không có thông tin thanh toán</span>';
            }

            // Tổng kết tiền
            const tongTien = hd.tongTien || 0;
            const giamGia = hd.giamGia || 0;
            const finalThanhTien = hd.thanhTien || (tongTien - giamGia);
            document.getElementById('detailTotalGoods').innerText = tongTien.toLocaleString('vi-VN') + 'đ';
            document.getElementById('detailDiscount').innerText = (giamGia > 0 ? '-' : '') + giamGia.toLocaleString('vi-VN') + 'đ';
            document.getElementById('detailFinalAmount').innerText = finalThanhTien.toLocaleString('vi-VN') + 'đ';

            // Hiển thị view chi tiết
            document.getElementById('invoiceListView').style.display = 'none';
            document.getElementById('invoiceDetailView').style.display = 'block';

        } catch (err) {
            console.error(err);
            alert('Lỗi tải chi tiết hóa đơn.');
        }
    }

    window.PosApp = {
        init,
        addToCart,
        updateQuantity,
        removeItem,
        clearCart,
        openInvoiceHistoryModal,
        showAgeModal: () => elements.ageModal.show()
    };
    return window.PosApp;
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', PosApp.init);
} else {
    PosApp.init();
}
