import { useQuery } from '@tanstack/react-query';
import { axiosInternal } from '../../lib/axios';
import useApiEndpoints from '../use-api-endpoints';

const useQueryOriginCities = () => {
  const endpoints = useApiEndpoints();
  return useQuery({
    queryKey: ['get-origin-city'],
    queryFn: () =>
      axiosInternal.get(endpoints.GLOBAL.GET_ORIGIN_CITIES_LIST).then((res) => res.data.data),
  });
};
export default useQueryOriginCities;
