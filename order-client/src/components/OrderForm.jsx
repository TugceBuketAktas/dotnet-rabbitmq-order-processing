import { useState } from 'react';
import '../styles/OrderForm.css';

const OrderForm = ({ onOrderSubmitted }) => {
  const [customerId, setCustomerId] = useState('');
  const [products, setProducts] = useState([{ productId: '', quantity: '', price: '' }]);
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState('');
  const [createdOrderId, setCreatedOrderId] = useState(null);

  const handleProductChange = (index, field, value) => {
    const newProducts = [...products];
    newProducts[index][field] = value;
    setProducts(newProducts);
  };

  const addProduct = () => {
    setProducts([...products, { productId: '', quantity: '', price: '' }]);
  };

  const removeProduct = (index) => {
    setProducts(products.filter((_, i) => i !== index));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setMessage('');

    try {
      if (!customerId) {
        setMessage('Müşteri ID gerekli');
        setLoading(false);
        return;
      }

      if (products.some(p => !p.productId || !p.quantity || !p.price)) {
        setMessage('Tüm ürün alanları zorunlu');
        setLoading(false);
        return;
      }

      const createOrderDto = {
        customerId: parseInt(customerId),
        items: products.map(p => ({
          productId: parseInt(p.productId),
          quantity: parseInt(p.quantity),
          price: parseFloat(p.price)
        }))
      };

      const response = await fetch('/api/orders', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(createOrderDto)
      });

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(errorText || 'Sipariş oluşturulamadı');
      }

      const orderData = await response.json();
      setCreatedOrderId(orderData.orderId);
      setMessage(`Sipariş alındı. İşlem ID: ${orderData.orderId}`);
      
      if (onOrderSubmitted) {
        onOrderSubmitted(orderData.orderId);
      }

      // Reset form
      setCustomerId('');
      setProducts([{ productId: '', quantity: '', price: '' }]);
    } catch (error) {
      setMessage(`Hata: ${error.message}`);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="order-form-container">
      <h2>Yeni Sipariş Oluştur</h2>
      <p className="demo-note">
        Demo akışı: Siparişi gönder, oluşan Sipariş ID ile durum sekmesinden kuyruğa düşüp işlenme adımlarını takip et.
      </p>
      <form onSubmit={handleSubmit}>
        <div className="form-group">
          <label>Müşteri ID:</label>
          <input
            type="number"
            value={customerId}
            onChange={(e) => setCustomerId(e.target.value)}
            placeholder="Müşteri ID"
            disabled={loading}
          />
        </div>

        <div className="products-section">
          <h3>Ürünler</h3>
          {products.map((product, index) => (
            <div
              key={index}
              className={`product-row ${products.length > 1 ? 'has-remove' : ''}`}
            >
              <input
                type="number"
                value={product.productId}
                onChange={(e) => handleProductChange(index, 'productId', e.target.value)}
                placeholder="Ürün ID"
                disabled={loading}
              />
              <input
                type="number"
                value={product.quantity}
                onChange={(e) => handleProductChange(index, 'quantity', e.target.value)}
                placeholder="Miktar"
                disabled={loading}
              />
              <input
                type="number"
                value={product.price}
                onChange={(e) => handleProductChange(index, 'price', e.target.value)}
                placeholder="Fiyat"
                step="0.01"
                disabled={loading}
              />
              {products.length > 1 && (
                <button
                  type="button"
                  onClick={() => removeProduct(index)}
                  className="btn-remove"
                  disabled={loading}
                >
                  Sil
                </button>
              )}
            </div>
          ))}
        </div>

        <button
          type="button"
          onClick={addProduct}
          className="btn-add-product"
          disabled={loading}
        >
          Ürün Ekle
        </button>

        <button
          type="submit"
          className="btn-submit"
          disabled={loading}
        >
          {loading ? 'Gönderiliyor...' : 'Siparişi Gönder'}
        </button>
      </form>

      {message && <div className={`message ${message.includes('Hata') ? 'error' : 'success'}`}>{message}</div>}
      {createdOrderId && (
        <div className="order-id-hint">
          Bu senaryoda arayacağın ID: <strong>{createdOrderId}</strong>
        </div>
      )}
    </div>
  );
};

export default OrderForm;
