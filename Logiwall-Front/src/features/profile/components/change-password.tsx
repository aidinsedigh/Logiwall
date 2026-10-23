import LgInput from 'components/ui/input';
import FormProvider from 'components/ui/form';
import { Stack } from '@mui/material';
import LgButton from 'components/ui/button';
import useChangePassword from '../api/change-password';

// --------------------------------------------------------------------------

const ChangePassword = () => {
  const { methods, HandleChangePassword, isPending } = useChangePassword();

  return (
    <FormProvider methods={methods} onSubmit={HandleChangePassword}>
      <Stack spacing={3}>
        <LgInput label="رمز عبور فعلی" name="oldPassword" />
        <LgInput label="رمز عبور جدید" name="newPassword" />
        <LgButton isLoading={isPending}>تغییر رمز عبور</LgButton>
      </Stack>
    </FormProvider>
  );
};
export default ChangePassword;
