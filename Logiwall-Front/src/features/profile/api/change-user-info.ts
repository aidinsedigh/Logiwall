import useApiEndpoints from 'hooks/use-api-endpoints';
import useQueryProfile from 'hooks/react-query/use-query-profile';
import { useMutation, UseMutationResult, useQueryClient } from '@tanstack/react-query';
import { axiosInternal } from 'lib/axios';
import { useNavigate } from 'react-router';
import { useForm } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import { toast } from 'sonner';
import * as yup from 'yup';
import { IchangeProfileInfo, IchangeProfileInfoResponse } from '../../../types/profile';

// --------------------------------------------------------------------------------------------

const schema = yup.object().shape({
  firstName: yup.string().required('نام خود را وارد کنید'),
  lastName: yup.string().required('نام خانوادگی خود را وارد کنید'),
  nationalCode: yup
    .string()
    .matches(/^\d*$/, 'کد ملی باید 10 رقمی باشد')
    .test('is-correct-length', 'کد ملی باید 10 رقمی باشد', (value) => {
      if (value && value.length > 0) {
        return value.length === 10;
      }
      return true;
    }),
  email: yup.string().email('لطفا ایمیل معتبر وارد کنید').required('ایمیل  خود را وارد کنید'),
  gender: yup.number().required('جنسیت خود را وارد کنید'),
});

// --------------------------------------------------------------------------------------------

const useChangeUserInfo = () => {
  const endpoints = useApiEndpoints();

  const queryClient = useQueryClient();

  const { data: profileInfo, isPending: profileInfoPending } = useQueryProfile();

  console.log(profileInfo);

  const { mutateAsync: UpdateUserInfo, isPending: updateUserInfoPending } = useMutation({
    mutationKey: ['update-user-info'],
    mutationFn: (payload) =>
      axiosInternal.post(endpoints.PROFILE.POST_UPDATE_PROFILE, payload).then((res) => res.data),
  }) as UseMutationResult<IchangeProfileInfoResponse>;

  const methods = useForm({
    resolver: yupResolver(schema),
    defaultValues: {
      firstName: profileInfo?.data?.firstName ?? '',
      lastName: profileInfo?.data?.lastName ?? '',
      nationalCode: profileInfo?.data?.nationalCode ?? 0,
      email: profileInfo?.data?.email ?? '',
      gender: profileInfo?.data?.gender ?? 1,
    },
  });
  const { handleSubmit } = methods;

  const Submit = handleSubmit(async (e) => {
    const mainPayload: IchangeProfileInfo = {
      ...profileInfo.data,
      ...e,
    };
    try {
      await UpdateUserInfo(mainPayload);
      queryClient.invalidateQueries({ queryKey: ['user-profile-info'] });
      toast.success('با موفقیت انجام شد');
    } catch (error) {
      console.log(error);
    }
  });
  return { Submit, methods, UpdateUserInfo, profileInfoPending, updateUserInfoPending };
};
export default useChangeUserInfo;
