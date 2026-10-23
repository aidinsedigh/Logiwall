import { useNavigate } from 'react-router';

//-----------------------------------------------------------------------------

const useQueryParam = () => {
  const navigate = useNavigate();

  const GetQuery = Object.fromEntries(new URLSearchParams(window.location.search));

  const setQuery = (newQuery: any) => {
    const searchParams = new URLSearchParams(newQuery);

    navigate(`${window.location.pathname}?${searchParams.toString()}`);
  };

  return { GetQuery, setQuery };
};

export default useQueryParam;
