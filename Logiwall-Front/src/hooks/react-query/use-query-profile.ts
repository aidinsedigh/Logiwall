import { useQuery } from '@tanstack/react-query';
import { axiosInternal } from '../../lib/axios';
import useApiEndpoints from '../use-api-endpoints';

const useQueryProfile = () => {
  const endpoints = useApiEndpoints();
  return useQuery({
    queryKey: ['user-profile-info'],
    queryFn: () => axiosInternal.get(endpoints.GLOBAL.GET_PROFILE_INFO).then((res) => res.data),
  });
};
export default useQueryProfile;
