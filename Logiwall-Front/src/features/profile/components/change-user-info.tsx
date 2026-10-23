import { Stack } from '@mui/material';
import LgAnimateLoading from 'components/ui/loading/animate-loading';
import FormProvider from 'components/ui/form';
import LgInput from 'components/ui/input';
import LgRadio from 'components/ui/radio';
import LgButton from 'components/ui/button';
import NumberField from 'components/ui/number-field';
import useChangeUserInfo from '../api/change-user-info';

// ---------------------------------------------------------------------------------------

const ChangeUserInfo = () => {
  const { Submit, methods, profileInfoPending, updateUserInfoPending } = useChangeUserInfo();

  return (
    <>
      {profileInfoPending ? (
        <LgAnimateLoading />
      ) : (
        <FormProvider methods={methods} onSubmit={Submit}>
          <Stack spacing={5}>
            <LgInput name="firstName" label="نام" />
            <LgInput name="lastName" label="نام خانوادگی" />
            <LgRadio
              name="gender"
              row
              options={[
                { label: 'اقا', value: 1 },
                { label: 'خانم', value: 3 },
              ]}
              label="جنسیت"
            />
            <NumberField name="nationalCode" label="کد ملی" />
            <LgInput name="email" label="ایمیل" />
            <LgButton type="submit" isLoading={updateUserInfoPending}>
              ثبت اطلاعات
            </LgButton>
          </Stack>
        </FormProvider>
      )}
      ;
    </>
  );
};
export default ChangeUserInfo;
