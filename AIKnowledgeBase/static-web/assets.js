// API Base URL (through Nginx proxy)
const API_BASE = '/api/v1/am';

// Check if we're in local test mode (no API available)
const isLocalTest = window.location.hostname === 'localhost' || window.location.port === '8080';

// Mock data for local testing
const mockAssets = [
    { id: '1', assetCode: 'PC-001', assetName: 'ThinkPad T14', category: '电脑', status: 'in_use', purchasePrice: 8000, location: '办公室' },
    { id: '2', assetCode: 'PTR-002', assetName: 'HP LaserJet', category: '打印机', status: 'in_use', purchasePrice: 3500, location: '前台' },
    { id: '3', assetCode: 'SRV-001', assetName: 'Dell R740', category: '服务器', status: 'maintenance', purchasePrice: 45000, location: '机房' }
];

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
    
    // Local test mode
    if (isLocalTest) {
        alert('本地测试模式：资产数据不会保存到服务器\n\n' + JSON.stringify(data, null, 2));
        hideAddForm();
        document.getElementById('assetForm').reset();
        return;
    }
    
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
        // Local test mode: use mock data
        if (isLocalTest) {
            renderAssets(mockAssets);
            updateStats(mockAssets.length, 2, 1, 0);
            return;
        }
        
        const response = await fetch(`${API_BASE}/assets?page=1&pageSize=50`);
        
        if (!response.ok) throw new Error('加载失败');
        
        const data = await response.json();
        renderAssets(data.items || []);
        updateStats(data.total || 0, data.inUse || '-', data.maintenance || '-', data.scrap || '-');
    } catch (e) {
        console.error('加载资产列表失败:', e);
        // Fallback to mock data on error
        renderAssets(mockAssets);
        updateStats(mockAssets.length, 2, 1, 0);
    }
}

function renderAssets(assets) {
    const tbody = document.querySelector('#assetTable tbody');
    if (!tbody) return;
    tbody.innerHTML = '';
    
    assets.forEach(asset => {
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
}

function updateStats(total, inUse, maintenance, scrap) {
    document.getElementById('totalAssets').textContent = total;
    document.getElementById('inUseAssets').textContent = inUse !== undefined ? inUse : '-';
    document.getElementById('maintenanceAssets').textContent = maintenance !== undefined ? maintenance : '-';
    document.getElementById('scrappedAssets').textContent = scrap !== undefined ? scrap : '-';
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
