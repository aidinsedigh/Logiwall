import FormProvider from 'components/ui/form';
import {
  Box,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  Stack,
  Typography,
} from '@mui/material';
import LgRadio from 'components/ui/radio';
import LgInput from 'components/ui/input';
import LgButton from 'components/ui/button';
import StepsButtons from '../../../components/ui/steps-buttons';
import * as React from 'react';
import LgIcon from 'components/ui/icon';
import usePay from '../api/pay';
import { IgoPayPayload } from '../../../types/create-order';

// --------------------------------------------------------------------

const StepPay = () => {
  const { payLoading, HandlePay, setDialogStatus, GoToPay, methods, GetQuery, dialogStatus } =
    usePay();

  return (
    <>
      <Box mt={5}>
        <Divider sx={{ my: 2 }}>
          <Box className="text-customColors-primaryOne bg-white p-4 rounded-3xl">
            <Typography>نحوه پرداخت</Typography>
          </Box>
        </Divider>
        <FormProvider methods={methods} onSubmit={HandlePay}>
          <LgRadio
            name="paymentType"
            defaultValue={1}
            options={[
              { label: 'پرداخت انلاین', value: 71 },
              { label: 'پس کرایه', value: 72 },
            ]}
          />
          <LgInput sx={{ mt: 3 }} name="description" label="توضیحات" />
        </FormProvider>
      </Box>

      <Box className="text-center mt-10">
        <LgButton fullWidth onClick={HandlePay} isLoading={payLoading}>
          {' '}
          <LgIcon icon="solar:card-bold-duotone" className="inline-block mx-0.5" /> پرداخت{' '}
        </LgButton>
      </Box>
      {/*<Dialog className="bg-white" open={dialogStatus} onClose={() => setDialogStatus(false)}>*/}
      {/*  <DialogTitle className="bg-white">ورودی برای پرداخت</DialogTitle>*/}
      {/*  <DialogContent className="bg-white p-5">*/}
      {/*    <Stack spacing={3} p={4}>*/}
      {/*      <Stack>*/}
      {/*        <Typography>orderId : {Number(GetQuery?.orderId)}</Typography>*/}
      {/*      </Stack>*/}
      {/*      /!*<Stack>*!/*/}
      {/*      /!*    <Typography>orderInqueryOptionId : {data?.data?.data[0].id}</Typography>*!/*/}
      {/*      /!*</Stack>*!/*/}
      {/*      <Stack>*/}
      {/*        <Typography>channel : {sessionStorage.getItem('channel')}</Typography>*/}
      {/*      </Stack>*/}
      {/*      <Stack>*/}
      {/*        <Typography>callbackUrl : {'http://app.logiwall.ir/app/home'}</Typography>*/}
      {/*      </Stack>*/}
      {/*    </Stack>*/}
      {/*  </DialogContent>*/}
      {/*  <DialogActions className="bg-white">*/}
      {/*    <LgButton*/}
      {/*      onClick={async () => {*/}
      {/*        const payload: IgoPayPayload = {*/}
      {/*          orderId: Number(GetQuery?.orderId),*/}
      {/*          pickupOptionId: Number(GetQuery?.pickupTime),*/}
      {/*          paymentType: 71,*/}
      {/*          channel: 'pwa',*/}
      {/*          callbackUrl: 'http://app.logiwall.ir/app/home',*/}
      {/*        };*/}
      {/*        try {*/}
      {/*          const response2 = await GoToPay(payload);*/}
      {/*          window.location.href = response2.paymentLink;*/}
      {/*        } catch (error) {*/}
      {/*          console.log(error);*/}
      {/*        }*/}
      {/*      }}*/}
      {/*      isLoading={payLoading}*/}
      {/*    >*/}
      {/*      ارسال به لینک پرداخت*/}
      {/*    </LgButton>*/}
      {/*    <LgButton onClick={() => setDialogStatus(false)}>cancel</LgButton>*/}
      {/*  </DialogActions>*/}
      {/*</Dialog>*/}
      <StepsButtons />
    </>
  );
};
export default StepPay;
