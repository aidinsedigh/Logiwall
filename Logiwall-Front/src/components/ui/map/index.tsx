import React, { FC, useEffect } from 'react';
import { MapContainer, Marker, TileLayer, useMap } from 'react-leaflet';
import 'leaflet/dist/leaflet.css';
import 'leaflet-geosearch/dist/geosearch.css';
import L from 'leaflet';
import marker from 'assets/images/map-pin-svgrepo-com.svg';
import { OpenStreetMapProvider, SearchControl } from 'leaflet-geosearch';
import { useWindowSize } from 'usehooks-ts';
import StepTitle from '../title';

//-----------------------------------------------------
const customCarIcon = new L.Icon({
  iconUrl: marker,
  iconSize: [70, 70],
  iconAnchor: [25, 50],
  popupAnchor: [0, -50],
  className: 'custom-car-icon',
});
//----------------------------------------------------

interface IBaMap {
  LatLong: [number, number];
  setLatLong: (value: any) => void;
  title: string;
}

const LgMap: FC<IBaMap> = ({ LatLong, setLatLong, title }) => {
  const { width } = useWindowSize();
  const SearchField = () => {
    const searchControl = new (SearchControl as any)({
      provider: new OpenStreetMapProvider({
        params: {
          'accept-language': 'ir',
          countrycodes: 'ir',
          addressdetails: 1,
        },
      }),
      style: width > 1000 ? 'bars' : 'bar',
      showMarker: false,
      showPopup: true,
      autoClose: true,
      retainZoomLevel: false,
      animateZoom: true,
      keepResult: false,
      searchLabel: 'جستجو...',
    });
    const map = useMap();

    useEffect((): any => {
      map.addControl(searchControl);
      return () => map.removeControl(searchControl);
    }, [map]);

    return null;
  };

  return (
    <>
      <div>
        <div id="map" className="bg-white w-full h-[250px]">
          <StepTitle
            isMapTitle
            icon={'wpf:airplanetakeoff'}
            className="text-customColors-primaryOne mx-2"
            title={title}
          />
          <MapContainer
            className="w-full h-[200px]"
            center={[35.70223, 51.324606]}
            zoom={13}
            scrollWheelZoom={false}
          >
            <TyleMap setlatLong={setLatLong} />
            <SearchField />
            <Marker
              icon={customCarIcon}
              position={LatLong && LatLong ? LatLong : [35.70223, 51.324606]}
            />
          </MapContainer>
        </div>
      </div>
    </>
  );
};

interface ItyleMap {
  setlatLong: (value: any) => void;
}

const TyleMap: FC<ItyleMap> = ({ setlatLong }) => {
  const map = useMap();

  useEffect(() => {
    map?.on('move', (e) => {
      setlatLong([e.target.getCenter().lat, e.target.getCenter().lng]);
    });
  });

  useEffect(() => {
    map.invalidateSize(true);
  });
  useEffect(() => {
    // Invalidate size when the map is fully rendered
    setTimeout(() => {
      map.invalidateSize();
    }, 300); // Add slight delay to ensure the map is fully loaded
  }, [map]);
  return <TileLayer url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" />;
};
export default LgMap;
