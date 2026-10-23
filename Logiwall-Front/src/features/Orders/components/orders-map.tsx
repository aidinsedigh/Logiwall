import { MapContainer, Marker, Polyline, TileLayer } from 'react-leaflet';
import { FC } from 'react';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';

//---------------------------------------------------------
interface IorderMaps {
  position1: [number, number];
  position2: [number, number];
}
//---------------------------------------------------------
const markerIcon = new L.Icon({
  iconUrl: 'https://leafletjs.com/examples/custom-icons/leaf-red.png', // You can set your custom marker URL here
  iconSize: [25, 41],
  iconAnchor: [12, 41],
  popupAnchor: [1, -34],
  shadowUrl: 'https://leafletjs.com/examples/custom-icons/leaf-shadow.png',
  shadowSize: [41, 41],
});
const OrdersMap: FC<IorderMaps> = ({ position1, position2 }) => {
  const polylinePositions = [position1, position2];

  return (
    <div>
      <div id="map" className="bg-white w-full h-[300px]">
        <MapContainer center={position1} zoom={10} style={{ height: '100%', width: '100%' }}>
          {/* Map Tile Layer */}
          <TileLayer
            url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
            attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
          />

          {/* Polyline to connect the two positions */}
          <Polyline positions={polylinePositions} color="blue" />

          {/* Marker for the starting point */}
          <Marker position={position1} icon={markerIcon}></Marker>

          {/* Marker for the ending point */}
          <Marker position={position2} icon={markerIcon}></Marker>
        </MapContainer>
      </div>
      ;
    </div>
  );
};
export default OrdersMap;
