// API Base URL (through Nginx proxy)
const API_BASE = '/api/v1/am';

// Show/hide forms
function showAddForm() {
    document.getElementById('addForm').classList.remove('hidden');
    document.getElementById('assets').classList.add('hidden');
}

function hideAddForm() {
    document.getElementById('addForm').classList.add('hidden');
    document.getElementById('assets').classList.remove('hidden');
}

// Submit form
async function submitForm() {
    const data = {
        assetCode: document.getElementById('assetCode').value,
        assetName: document.getElementById('assetName').value,
        assetType: document.getElementById('assetType').value,
        category: document.getElementById('category').value,
        purchaseDate: document.getElementById('purchaseDate').value,
        purchasePrice: parseFloat(document.getElementById('purchasePrice').value) || 0,
        vendor: document.getElementById('vendor').value,
        location: document.getElementById('location').value,
        specs: document.getElementById('specs').value
    };
    
    try {
        const response = await fetch(`${API_BASE}/assets`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': 'Bearer ' + localStorage.getItem('token')
            },
            body: JSON.stringify(data)
        });
        
        if (response.ok) {
            alert('资产创建成功');
            loadAssets();
            hideAddForm();
            document.getElementById('assetForm').reset();
        } else {
            const error = await response.text();
            alert('创建失败: ' + error);
        }
    } catch (e) {
        alert('网络错误: ' + e.message);
    }
}

// Load assets
async function loadAssets() {
    try {
        const response = await fetch(`${API_BASE}/assets?page=1&pageSize=50`, {
            headers: {
                'Authorization': 'Bearer ' + localStorage.getItem('token')
            }
        });
        
        if (!response.ok) throw new Error('加载失败');
        
        const data = await response.json();
        const tbody = document.querySelector('#assetTable tbody');
        tbody.innerHTML = '';
        
        data.items.forEach(asset => {
            const tr = document.createElement('tr');
            tr.innerHTML = `
                <td>${asset.assetCode}</td>
                <td>${asset.assetName}</td>
                <td>${asset.category || '-'}</td>
                <td><span class="badge ${getStatusClass(asset.status)}">${getStatusText(asset.status)}</span></td>
                <td>${asset.purchasePrice || '-'}</td>
                <td>${asset.location || '-'}</td>
                <td>
                    <button onclick="viewAsset('${asset.id}')">查看</button>
                    <button onclick="editAsset('${asset.id}')">编辑</button>
                </td>
            `;
            tbody.appendChild(tr);
        });
        
        // Update stats
        updateStats(data.total || 0);
    } catch (e) {
        console.error('加载资产列表失败:', e);
    }
}

function updateStats(total) {
    document.getElementById('totalAssets').textContent = total;
    document.getElementById('inUseAssets').textContent = '-';
    document.getElementById('maintenanceAssets').textContent = '-';
    document.getElementById('scrappedAssets').textContent = '-';
}

function getStatusClass(status) {
    const map = { 'in_use': 'success', 'maintenance': 'warning', 'scrap': 'danger' };
    return map[status] || 'default';
}

function getStatusText(status) {
    const map = { 'in_use': '使用中', 'maintenance': '维修中', 'scrap': '已报废' };
    return map[status] || status;
}

function viewAsset(id) {
    window.location.href = `#assets/view?id=${id}`;
}

function editAsset(id) {
    window.location.href = `#assets/edit?id=${id}`;
}

// Load on page load
if (document.getElementById('assetTable')) {
    loadAssets();
}
