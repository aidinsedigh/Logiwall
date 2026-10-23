import LgHeader from '../header';
import { Box } from '@mui/material';
import { FC } from 'react';
import { Ichildren } from '../../../types/global';

const HomeLayout: FC<Ichildren> = ({ children }) => {
  return (
    <>
      <LgHeader />
      <Box className="container mx-auto mt-10 mx-2 px-4 md:px-32">{children}</Box>
    </>
  );
};
export default HomeLayout;
