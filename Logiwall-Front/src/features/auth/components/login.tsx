import { Box, Checkbox, FormControlLabel, Link, Typography } from '@mui/material';
import { Controller } from 'react-hook-form';
import { FC, useEffect } from 'react';
import { useNavigate } from 'react-router';
import LgIcon from 'components/ui/icon';
import FormProvider from 'components/ui/form';
import LgInput from 'components/ui/input';
import LgButton from 'components/ui/button';
import useLogin from 'features/auth/api/login';
import useQueryParam from 'hooks/use-query-param';
import { useSearchParams } from 'react-router-dom';

//--------------------------------------------------------------

const Login: FC = () => {
  const { methods, HandleSubmitLogin, control, LoginPending } = useLogin();
  const navigate = useNavigate();
  const { GetQuery } = useQueryParam();
  const [searchParams] = useSearchParams();

  useEffect(() => {
    if (GetQuery?.channel) {
      sessionStorage.setItem('channel', GetQuery?.channel);
    }
  }, []);

  return (
    <>
      <FormProvider onSubmit={HandleSubmitLogin} methods={methods}>
        <Box className="space-y-5 md:px-32">
          <LgInput name="userName" disabled={!!GetQuery?.channel} label="نام کاربری" />
          <LgInput name="password" label="رمز عبور" />
          <Box className="text-right">
            <Controller
              name="rememberMe"
              control={control}
              render={({ field }) => (
                <FormControlLabel
                  className="text-base"
                  control={<Checkbox {...field} checked={field.value} size="small" />}
                  label={<p className="text-xs">من را بخاطر بسپار</p>}
                />
              )}
            />
          </Box>
          <Box>
            <LgButton isLoading={LoginPending} type="submit" className="w-full md:w-96">
              ورود به لاجی وال
            </LgButton>
          </Box>
          <Box>
            <Link
              className="flex justify-center cursor-pointer"
              onClick={() => {
                const queryString = searchParams.toString();
                navigate(queryString ? `/auth/otp-login?${queryString}` : '/auth/otp-login', {
                  replace: true,
                });
              }}
            >
              <LgIcon icon="lets-icons:message-duotone" />{' '}
              <Typography mx={0.5}>ورود با رمز یکبار مصرف</Typography>
            </Link>
          </Box>
          {GetQuery?.channel && (
            <Box>
              <Link className="flex justify-center cursor-pointer" href={'http://app.logiwall.ir/'}>
                <Typography> ورود مستقیم با شماره دیگر </Typography>
              </Link>
            </Box>
          )}
        </Box>
      </FormProvider>
    </>
  );
};
export default Login;
