import { Box, Typography } from '@mui/material';
import { FC } from 'react';
import LgIcon from '../icon';

//--------------------------------------------------
interface IstepTitle {
  title: string;
  icon?: string;
  className?: string;
  isMapTitle?: boolean;
}
//------------------------------------------------------

const StepTitle: FC<IstepTitle> = ({ title, icon, className, isMapTitle = false }) => {
  return (
    <>
      <Box className={`${isMapTitle ? 'py-2.5' : 'pt-2 pb-8'}`}>
        <Typography fontSize={14}>
          {' '}
          <LgIcon width={20} icon={icon} className={`inline-block ${className}`} /> {title}{' '}
        </Typography>
      </Box>
    </>
  );
};
export default StepTitle;
