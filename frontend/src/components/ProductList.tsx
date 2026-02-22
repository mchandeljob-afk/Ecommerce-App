import { useState, useEffect } from 'react';
import { Product } from '../types';
import { productApi, cartApi } from '../services/api';
import axios from 'axios';

interface Props {
  cartId: string | null;
  onAddToCart: (cartId: string) => void;
}

export default function ProductList({ cartId, onAddToCart }: Props) {
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [addingProduct, setAddingProduct] = useState<number | null>(null);

  useEffect(() => {
    loadProducts();
  }, []);

  const loadProducts = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await productApi.getAll();
      setProducts(data);
    } catch (err) {
      setError('Failed to load products. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const handleAddToCart = async (productId: number) => {
    try {
      setAddingProduct(productId);
      const result = await cartApi.addItem(productId, 1, cartId || undefined);
      onAddToCart(result.cartId);
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.data?.error) {
        alert(err.response.data.error);
      } else {
        alert('Failed to add item to cart.');
      }
    } finally {
      setAddingProduct(null);
    }
  };

  if (loading) {
    return (
      <div className="loading">
        <div className="spinner" />
        <p>Loading products...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div>
        <div className="error-msg">{error}</div>
        <button className="add-btn" style={{ maxWidth: 200 }} onClick={loadProducts}>
          Retry
        </button>
      </div>
    );
  }

  return (
    <div>
      <h2>Product Catalog</h2>
      <div className="product-grid">
        {products.map(product => (
          <div key={product.id} className="product-card">
            <div className="product-category">{product.category}</div>
            <div className="product-name">{product.name}</div>
            <div className="product-desc">{product.description}</div>
            <div className="product-price">₹{product.price.toFixed(2)}</div>
            <div className={`product-stock ${product.stock < 10 ? 'low' : ''}`}>
              {product.stock > 0 ? `${product.stock} in stock` : 'Out of stock'}
            </div>
            <button
              className="add-btn"
              disabled={product.stock === 0 || addingProduct === product.id}
              onClick={() => handleAddToCart(product.id)}
            >
              {addingProduct === product.id ? 'Adding...' : 'Add to Cart'}
            </button>
          </div>
        ))}
      </div>
    </div>
  );
}
