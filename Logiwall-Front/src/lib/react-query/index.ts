import { QueryClient } from '@tanstack/react-query';

//---------------------------------------------------------------------------------------------

export const queryClientConfig = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: false,
    },
  },
});
