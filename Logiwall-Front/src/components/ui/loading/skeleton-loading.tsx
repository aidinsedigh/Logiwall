import { Skeleton, SkeletonProps, Stack } from '@mui/material';
import { FC } from 'react';

// ---------------------------------------------------------

interface IlgSkeleton extends SkeletonProps {
  rowsNumber: number;
}

// ---------------------------------------------------------
const LgSkeletonLoading: FC<IlgSkeleton> = ({ rowsNumber, ...props }) => {
  return (
    <>
      <Stack my={5} spacing={2}>
        {Array.from({ length: rowsNumber }).map((_, index) => (
          <Skeleton
            key={index}
            sx={{ borderRadius: 5 }}
            variant="rectangular"
            width="100%"
            height="150px"
            animation="wave"
            {...props}
          />
        ))}
      </Stack>
    </>
  );
};
export default LgSkeletonLoading;
