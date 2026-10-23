import { Box, Typography } from '@mui/material';
import { useEffect } from 'react';
import { useCountdown } from 'usehooks-ts';
import useOtpLogin from 'features/auth/api/otp';
import useQueryParam from 'hooks/use-query-param';
import StepTitle from 'components/ui/title';
import { convertSecondsToMinutes } from 'utils/utils';
import LgButton from 'components/ui/button';
import FormProvider from 'components/ui/form';
import OtpInput from 'components/ui/otp-input';
import LgIcon from 'components/ui/icon';
import { useNavigate } from 'react-router';
import { useSearchParams } from 'react-router-dom';

//---------------------------------------------------------------------------------------------

const OtpConfirmCode = () => {
  const [count, { resetCountdown, startCountdown }] = useCountdown({ countStart: 2 * 60 });
  const navigate = useNavigate();
  const {
    HandleConfirmOtpCode,
    ResendOtpCode,
    sendOtpPending,
    confirmOtpPending,
    confirmOtpControl,
    confirmOtpMethods,
  } = useOtpLogin();

  const { GetQuery } = useQueryParam();
  const [searchParams] = useSearchParams();

  useEffect(() => {
    startCountdown();
  }, [startCountdown]);

  return (
    <>
      <StepTitle title="کد ارسالی به شماره همراه خود را وارد کنید" />
      <Box mb={5}>
        {count > 0 ? (
          <Typography variant="body2">
            {' '}
            {convertSecondsToMinutes(count)} تا منقضی شدن کد ارسالی{' '}
          </Typography>
        ) : (
          <LgButton
            onClick={() => {
              ResendOtpCode();
              resetCountdown();
              startCountdown();
            }}
            isLoading={sendOtpPending}
          >
            ارسال مجدد کد
          </LgButton>
        )}
      </Box>
      <FormProvider methods={confirmOtpMethods} onSubmit={HandleConfirmOtpCode}>
        <OtpInput
          onComplete={() => HandleConfirmOtpCode()}
          name="code"
          control={confirmOtpControl}
        />
        <Box mt={5} className="flex justify-center">
          <Box
            className="flex text-customColors-primaryOne cursor-pointer"
            onClick={() => {
              if (GetQuery?.channel) {
                const queryString = searchParams.toString();
                navigate(`/auth/login?${queryString}`, { replace: true });
              } else {
                navigate('/auth/otp-login');
              }
            }}
          >
            <LgIcon
              width={20}
              className="cursor-pointer mt-2"
              icon="solar:alt-arrow-right-line-duotone"
            />
            <Typography mt={1} fontSize={12}>
              بازگشت
            </Typography>
          </Box>
          <Box ml={10}>
            <LgButton type="submit" isLoading={confirmOtpPending}>
              تایید
            </LgButton>
          </Box>
        </Box>
      </FormProvider>
    </>
  );
};
export default OtpConfirmCode;
