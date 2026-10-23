import { FC } from 'react';
import { Ichildren } from '../types/global';
import { QueryClientProvider } from '@tanstack/react-query';
import { queryClientConfig } from 'lib/react-query';
import MuiProvider from '../lib/mui';
import { Provider } from 'react-redux';
import { Store } from '../store/store';
import { Toaster } from 'sonner';
import { AuthProvider } from '../lib/auth';

//------------------------------------------------------------------------------------

const Providers: FC<Ichildren> = ({ children }) => {
  return (
    <QueryClientProvider client={queryClientConfig}>
      <Provider store={Store}>
        <MuiProvider>
          <AuthProvider>
            <Toaster richColors dir="rtl" position="top-center" closeButton />
            {children}
          </AuthProvider>
        </MuiProvider>
      </Provider>
    </QueryClientProvider>
  );
};
export default Providers;
