async function submitForm() {
    const data = {
        assetCode: document.getElementById('assetCode').value,
        assetName: document.getElementById('assetName').value,
        assetType: document.getElementById('assetType').value,
        category: document.getElementById('category').value,
        departmentId: document.getElementById('departmentId').value,
        userId: document.getElementById('userId').value,
        purchaseDate: document.getElementById('purchaseDate').value,
        purchasePrice: parseFloat(document.getElementById('purchasePrice').value) || 0,
        vendor: document.getElementById('vendor').value,
        location: document.getElementById('location').value,
        specs: document.getElementById('specs').value
    };
    
    try {
        const response = await fetch('http://115.29.233.46:5000/api/v1/am/assets', {
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
            document.getElementById('assetForm').reset();
        } else {
            const error = await response.text();
            alert('创建失败: ' + error);
        }
    } catch (e) {
        alert('网络错误: ' + e.message);
    }
}

async function loadAssets() {
    try {
        const response = await fetch('http://115.29.233.46:5000/api/v1/am/assets?page=1&pageSize=50', {
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
    } catch (e) {
        console.error('加载资产列表失败:', e);
    }
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
