import { Suspense } from 'react';
import { ErrorBoundary } from 'react-error-boundary';
import { Outlet, useLocation } from 'react-router-dom';
import AuthLayout from '../../components/layouts/auth';

//--------------------------------------------------------------

const AuthRoot = () => {
  const location = useLocation();
  return (
    <>
      <AuthLayout>
        <Suspense>
          <ErrorBoundary key={location.pathname} fallback={<div>Something went wrong!</div>}>
            <Outlet />
          </ErrorBoundary>
        </Suspense>
      </AuthLayout>
    </>
  );
};
export default AuthRoot;
