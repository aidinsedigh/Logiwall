import { FC, forwardRef } from 'react';
import { Box } from '@mui/material';
import { Icon } from '@iconify/react';

//-----------------------------------------------------------------
interface Iicon {
  ref?: any;
  width?: number;
  sx?: object;
  icon?: any;
  className?: string;
  onClick?: () => void;
}
//-----------------------------------------------------------------

const LgIcon: FC<Iicon> = forwardRef(
  ({ icon, onClick, className, width = 20, sx, ...other }: Iicon, ref) => (
    <Box
      onClick={onClick}
      ref={ref}
      component={Icon}
      className={`component-iconify ${className}`}
      icon={icon}
      sx={{ width, height: width, ...sx }}
      {...other}
    />
  )
);

export default LgIcon;
