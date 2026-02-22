import axios from 'axios';
import { Product, Cart, Order, AddCartItemResponse, CouponResult } from '../types';

const api = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' },
});

export const productApi = {
  getAll: () => api.get<Product[]>('/products').then(r => r.data),
};

export const cartApi = {
  addItem: (productId: number, quantity: number, cartId?: string) =>
    api.post<AddCartItemResponse>('/cart/items', { productId, quantity, cartId }).then(r => r.data),

  getCart: (cartId: string) =>
    api.get<Cart>(`/cart/${cartId}`).then(r => r.data),

  applyCoupon: (cartId: string, couponCode: string) =>
    api.post<CouponResult>(`/cart/${cartId}/apply-coupon`, { couponCode }).then(r => r.data),

  checkout: (cartId: string) =>
    api.post<Order>(`/cart/${cartId}/checkout`).then(r => r.data),
};

export const orderApi = {
  getOrder: (orderId: string) =>
    api.get<Order>(`/orders/${orderId}`).then(r => r.data),
};
