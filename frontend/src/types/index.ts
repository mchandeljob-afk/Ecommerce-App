export interface Product {
  id: number;
  name: string;
  price: number;
  stock: number;
  description: string;
  category: string;
}

export interface CartItem {
  id: number;
  productId: number;
  productName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface Cart {
  id: string;
  items: CartItem[];
  subtotal: number;
  appliedCouponCode: string | null;
  discountAmount: number;
  total: number;
}

export interface OrderItem {
  productId: number;
  productName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface Order {
  orderId: string;
  items: OrderItem[];
  subtotal: number;
  discount: number;
  couponCode: string | null;
  tax: number;
  grandTotal: number;
  status: string;
  createdAt: string;
}

export interface AddCartItemResponse {
  cartId: string;
  message: string;
}

export interface CouponResult {
  success: boolean;
  message: string;
  discountAmount: number;
  couponCode: string;
}

export interface ApiError {
  error: string;
  detail?: string;
  validationErrors?: Record<string, string[]>;
}
