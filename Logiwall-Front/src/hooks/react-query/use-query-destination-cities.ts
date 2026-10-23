import { useQuery } from '@tanstack/react-query';
import { axiosInternal } from '../../lib/axios';
import useApiEndpoints from '../use-api-endpoints';

const useQueryDestinationCities = () => {
  const endpoints = useApiEndpoints();
  return useQuery({
    queryKey: ['get-destination-city'],
    queryFn: () =>
      axiosInternal.get(endpoints.GLOBAL.GET_DESTINATION_CITIES_LIST).then((res) => res.data.data),
  });
};
export default useQueryDestinationCities;
