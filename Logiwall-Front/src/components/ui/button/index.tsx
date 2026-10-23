import { Button, ButtonProps, CircularProgress } from '@mui/material';
import { FC, ReactNode } from 'react';

//--------------------------------------------------------
interface ilgButton extends ButtonProps {
  children: ReactNode;
  backgroundColor?: string;
  isLoading?: boolean;
}
//---------------------------------------------------------

const LgButton: FC<ilgButton> = ({ children, isLoading = false, backgroundColor, ...props }) => {
  return (
    <Button
      type="submit"
      sx={{
        borderRadius: 3,
        border: 'none',
        px: 5,
        py: 1.5,
        boxShadow: 'none',
      }}
      size="large"
      variant="contained"
      disabled={isLoading}
      {...props}
    >
      {isLoading ? <CircularProgress size={18} /> : children}
    </Button>
  );
};
export default LgButton;
