import axios, { AxiosError, AxiosInstance, AxiosResponse } from 'axios';
import { toast } from 'sonner';

//-------------------------------------------------------------------------------

// axios auth instance (Auth Instance)
export const axiosAuth: AxiosInstance = axios.create({
  baseURL: process.env.REACT_APP_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// axios Auth response interceptor
axiosAuth.interceptors.response.use(
  (response: AxiosResponse) => {
    return response;
  },
  (error: any) => {
    if (error.response) {
      if (error.response.data.message) {
        toast.error(error.response.data.message);
      } else {
        toast.error('خطایی رخ داده');
      }
    } else {
      toast.error('خطایی رخ داده');
    }
    return Promise.reject(error);
  }
);

/////////////////////////////////////////////////////////////////////////////////////////////////////////////

// axios internal instance (Internal Instance)
export const axiosInternal: AxiosInstance = axios.create({
  baseURL: process.env.REACT_APP_BASE_URL,
  timeout: 10000,
  headers: {},
});

// axios internal request interceptor
axiosInternal.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error: AxiosError) => {
    return Promise.reject(error);
  }
);

// axios internal response interceptor
axiosInternal.interceptors.response.use(
  (response: AxiosResponse) => {
    return response;
  },
  (error: any) => {
    if (error.response) {
      if (error.response.data.message) {
        toast.error(error.response.data.message);
      } else {
        toast.error('خطایی رخ داده');
      }
    } else {
      toast.error('خطایی رخ داده');
    }
    return Promise.reject(error);
  }
);
