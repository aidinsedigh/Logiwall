import { Box, Stack, Typography } from '@mui/material';
import noData from 'assets/images/undraw_mobile_search_jxq5.svg';
import { FC } from 'react';

// ------------------------------------------------------------------------------------

interface InotDataFound {
  size?: number;
}

const NoDataFound: FC<InotDataFound> = ({ size }) => {
  return (
    <Stack textAlign="center" spacing={2} mt={10}>
      <Box className="text-center">
        <img
          src={noData}
          style={{ width: size ?? 130 }}
          className="mr-5 inline-block"
          alt="No Data Found"
        />
      </Box>
      <Typography color="gray">داده ای یافت نشد !</Typography>
    </Stack>
  );
};
export default NoDataFound;
