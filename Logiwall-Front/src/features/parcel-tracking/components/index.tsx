import { Box, Typography } from '@mui/material';
import { useWindowSize } from 'usehooks-ts';
import FormProvider from '../../../components/ui/form';
import LgInput from '../../../components/ui/input';
import LgButton from '../../../components/ui/button';
import useTracking from '../api/tracking';

//-------------------------------------------------------------------------

const ParcelTracking = () => {
  const { width } = useWindowSize();
  const { OnSubmit, methods, isPending } = useTracking();
  return (
    <>
      <Box>
        <Typography className="text-gray-500" fontSize={width < 900 ? 13 : 17} mx={1}>
          کد رهگیری خود را وارد کنید.
        </Typography>
        <FormProvider onSubmit={OnSubmit} methods={methods}>
          <div className="text-center">
            <LgInput
              sx={{ mt: 5, width: width < 900 ? '100%' : '400px' }}
              label="کد رهگیری"
              name="Code"
            />
            <Box>
              <LgButton type="submit" isLoading={isPending} fullWidth={width < 900} sx={{ mt: 5 }}>
                رهگیری کن
              </LgButton>
            </Box>
          </div>
        </FormProvider>
      </Box>
    </>
  );
};
export default ParcelTracking;
