import { createBrowserRouter, RouterProvider } from 'react-router-dom';
import { QueryClient, useQueryClient } from '@tanstack/react-query';
import { lazy, Suspense, useMemo } from 'react';
import AppRoot from './routes/root';
import LgAnimateLoading from '../components/ui/loading/animate-loading';
import { AuthGuard, GuestGuard } from '../lib/auth/hooks/auth-guard';
import AuthRoot from './routes/auth-root';

//----------------------------------------------------------------------------------------------------
const Lazy404Page = lazy(() =>
  import('./routes/404').then((module) => ({ default: module.Error404Page }))
);
const LazyLoginPage = lazy(() =>
  import('./routes/auth/login').then((module) => ({ default: module.LoginPage }))
);
const LazyOtpLoginPage = lazy(() =>
  import('./routes/auth/otp-login').then((module) => ({ default: module.OtpLoginPage }))
);
const LazyRegisterPage = lazy(() =>
  import('./routes/auth/register').then((module) => ({ default: module.RegisterPage }))
);
const LazyHomePage = lazy(() =>
  import('./routes/home').then((module) => ({ default: module.HomePage }))
);
const LazyCreateOrderPage = lazy(() =>
  import('./routes/create-order').then((module) => ({ default: module.CreateOrderPage }))
);
const LazyProfilePage = lazy(() =>
  import('./routes/profile').then((module) => ({ default: module.ProfilePage }))
);
const LazyChangePasswordPage = lazy(() =>
  import('./routes/profile/change-password').then((module) => ({
    default: module.ChangePasswordPage,
  }))
);
const LazyParcelTrackingPage = lazy(() =>
  import('./routes/parcel-tracking').then((module) => ({ default: module.ParcelTrackingPage }))
);
const LazyParcelTrackingResultPage = lazy(() =>
  import('./routes/parcel-tracking-result').then((module) => ({
    default: module.ParcelTrackingPage,
  }))
);
const LazyOrdersPage = lazy(() =>
  import('./routes/orders').then((module) => ({ default: module.OrdersPage }))
);
//----------------------------------------------------------------------------------------------------

export const createAppRouter = (queryClient: QueryClient) =>
  createBrowserRouter([
    {
      path: '/',
      element: (
        <GuestGuard>
          <></>
        </GuestGuard>
      ),
    },

    {
      path: '/auth',
      element: (
        <GuestGuard>
          <AuthRoot />
        </GuestGuard>
      ),
      children: [
        {
          path: 'login',
          element: <LazyLoginPage />,
        },
        {
          path: 'otp-login',
          element: <LazyOtpLoginPage />,
        },
        {
          path: 'register',
          element: <LazyRegisterPage />,
        },
      ],
    },

    {
      path: '/app',
      element: (
        <AuthGuard>
          <AppRoot />
        </AuthGuard>
      ),
      children: [
        {
          path: 'home',
          element: <LazyHomePage />,
        },
        {
          path: 'create-order',
          element: <LazyCreateOrderPage />,
        },
        {
          path: 'profile',
          element: <LazyProfilePage />,
        },
        {
          path: 'change-password',
          element: <LazyChangePasswordPage />,
        },
        {
          path: 'parcel-tracking',
          element: <LazyParcelTrackingPage />,
        },
        {
          path: 'parcel-tracking/result',
          element: <LazyParcelTrackingResultPage />,
        },
        {
          path: 'orders',
          element: <LazyOrdersPage />,
        },
      ],
    },
    {
      path: '*',
      element: (
        <Suspense fallback={<LgAnimateLoading />}>
          <Lazy404Page />
        </Suspense>
      ),
    },
  ]);

const App = () => {
  const queryClient = useQueryClient();
  const router = useMemo(() => createAppRouter(queryClient), [queryClient]);

  return <RouterProvider router={router} />;
};

export default App;
