import { createTheme, CssBaseline, ThemeProvider } from '@mui/material';
import { createContext, FC, ReactNode, useContext, useEffect, useMemo, useState } from 'react';
import rtlPlugin from 'stylis-plugin-rtl';
import { CacheProvider } from '@emotion/react';
import createCache from '@emotion/cache';
import { Ichildren } from '../../types/global';
import { palette } from './palette';
import { components } from './component';

// -------------------------------------------------------------------------------------------------

const ThemeContext = createContext({ toggleTheme: () => {} });
export const useThemeContext = () => useContext(ThemeContext);

const MuiProvider: FC<Ichildren> = ({ children }) => {
  const getQueryTheme: any = () => {
    const params = new URLSearchParams(window.location.search);
    const theme = params.get('theme');
    return theme === 'dark' || theme === 'light' ? theme : null;
  };

  // Read the initial mode from query param or localStorage, default to 'light'
  const getInitialMode = () => {
    const storedMode = localStorage.getItem('theme');
    const queryTheme = getQueryTheme();
    return queryTheme || storedMode || 'light';
  };

  const [mode, setMode] = useState<'light' | 'dark'>(getInitialMode());

  useEffect(() => {
    // Save mode to localStorage when it changes
    localStorage.setItem('theme', mode);
  }, [mode]);

  const memoizedValue: any = useMemo(
    () => ({
      typography: {
        fontFamily: 'iranYekan',
        fontSize: 10,
      },
      direction: 'rtl',
      palette: {
        ...palette(mode),
      },
      components: components,
    }),
    [mode]
  );

  const theme = createTheme(memoizedValue);

  const toggleTheme = () => {
    setMode((prevMode: 'light' | 'dark') => (prevMode === 'light' ? 'dark' : 'light'));
  };
  return (
    <ThemeContext.Provider value={{ toggleTheme }}>
      <ThemeProvider theme={theme}>
        <RTL>
          <CssBaseline />
          {children}
        </RTL>
      </ThemeProvider>
    </ThemeContext.Provider>
  );
};
export default MuiProvider;

// Create rtl cache
const cacheRtl = createCache({
  key: 'muirtl',
  stylisPlugins: [rtlPlugin],
});

function RTL({ children }: { children: ReactNode }) {
  return <CacheProvider value={cacheRtl}>{children}</CacheProvider>;
}
