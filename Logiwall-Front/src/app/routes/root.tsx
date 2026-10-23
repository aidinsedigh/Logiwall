import { Suspense } from 'react';
import { ErrorBoundary } from 'react-error-boundary';
import { Outlet, useLocation } from 'react-router-dom';
import LgAnimateLoading from 'components/ui/loading/animate-loading';
import HomeLayout from '../../components/layouts/home';

//--------------------------------------------------------------

const AppRoot = () => {
  const location = useLocation();
  return (
    <>
      <HomeLayout>
        <Suspense fallback={<LgAnimateLoading />}>
          <ErrorBoundary key={location.pathname} fallback={<div>Something went wrong!</div>}>
            <Outlet />
          </ErrorBoundary>
        </Suspense>
      </HomeLayout>
    </>
  );
};
export default AppRoot;
